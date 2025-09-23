using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Dispatching;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DeskBand11
{
    // READ ME
    // GO HERE
    //
    // https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-supportmenuitemcontroltype

    public partial class MenuItemsBand : TaskbarItemViewModel, IDisposable
    {
        public override string Id => "builtin.MenuItemsBand";

        private DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();

        public MenuItemsBand()
        {
            Title = "Menu Items";

            AnonymousCommand listMenuItemsCommand = new(() =>
            {
                // This may take a little time, so run in background thread
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await Task.Delay(1000); // slight delay to allow user to switch to target app if needed
                    int result = UiaTools.ListMenuItems();
                    _queue.TryEnqueue(() =>
                    {
                        Title = result < 0 ?
                            $"Error listing menu items: {result}" :
                            result == 0 ?
                                "Found no items" :
                                $"Found {result} items";
                    });
                });
            })
            { Name = "List Menu Items", Icon = new("\uE7C3") };

            Buttons.Add(new CommandViewModel(listMenuItemsCommand));
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    class UiaTools
    {
        // P/Invoke to get the current foreground window
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // Constants (from UIAutomationClient.h)
        private const int UIA_ControlTypePropertyId = 30003; // ControlType property id
        //private const int UIA_MenuItemControlTypeId = 50018; // common ControlType id for MenuItem (ControlType IDs start at 50000 series)
        // TreeScope flags (from uiautomationclient.h)
        private const int TreeScope_Element = 0x1;
        private const int TreeScope_Children = 0x2;
        private const int TreeScope_Descendants = 0x4;
        private const int TreeScope_Subtree = TreeScope_Element | TreeScope_Children | TreeScope_Descendants;

        // CLSID / IID values verified from MS docs:
        // CLSID_CUIAutomation   = {FF48DBA4-60EF-4201-AA87-54103EEF594E}
        // IID_IUIAutomation     = {30CBE57D-D9D0-452A-AB13-7AC5AC4825EE}
        private static readonly Guid CLSID_CUIAutomation = new("FF48DBA4-60EF-4201-AA87-54103EEF594E");
        private static readonly Guid IID_IUIAutomation = new("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE");

        public static int ListMenuItems()
        {
            // Create the UIAutomation COM object
            object? raw = null;
            IUIAutomation? automation = null;

            try
            {
                // Create COM instance of CUIAutomation
                // Type.GetTypeFromCLSID is allowed in AOT if used to create a COM object and the COM interface is statically defined.
                Type t = Type.GetTypeFromCLSID(CLSID_CUIAutomation, throwOnError: true);
                raw = Activator.CreateInstance(t); // returns a RCW that implements IUIAutomation
                automation = (IUIAutomation)raw;

                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    Debug.WriteLine("No foreground window found.");
                    return -1;
                }

                // Get element for the HWND
                //int hr = automation.ElementFromHandle(hwnd, out IUIAutomationElement rootElem);
                int hr = 0;
                IUIAutomationElement rootElem = automation.ElementFromHandle((HWND)hwnd);
                if (hr != 0 || rootElem == null)
                {
                    Debug.WriteLine($"ElementFromHandle failed HRESULT=0x{hr:X8}");
                    return -2;
                }

                // Build a property condition: ControlType == MenuItem
                // UIA control type ids (standard ones) are used (MenuItem is 50018).
                //hr = automation.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_MenuItemControlTypeId, out IUIAutomationCondition menuItemCond);
                //IUIAutomationCondition menuItemCond = automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_ControlTypePropertyId, UIA_MenuItemControlTypeId);
                IUIAutomationCondition menuItemCond = automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_ControlTypePropertyId, UIA_CONTROLTYPE_ID.UIA_MenuItemControlTypeId);
                if (hr != 0 || menuItemCond == null)
                {
                    Debug.WriteLine($"CreatePropertyCondition failed HRESULT=0x{hr:X8}");
                    ReleaseIfNotNull(rootElem);
                    return -3;
                }

                // Find all menu items in the subtree of the window
                //hr = rootElem.FindAll(TreeScope_Subtree, menuItemCond, out IUIAutomationElementArray found);
                IUIAutomationElementArray found = rootElem.FindAll(TreeScope.TreeScope_Subtree | TreeScope.TreeScope_Ancestors, menuItemCond);
                if (hr != 0 || found == null)
                {
                    Debug.WriteLine($"FindAll failed HRESULT=0x{hr:X8}");
                    ReleaseIfNotNull(menuItemCond);
                    ReleaseIfNotNull(rootElem);
                    return -4;
                }

                // Get length and iterate

                //found.get_Length(out int len);
                int len = found.Length;
                Debug.WriteLine($"Found {len} MenuItem(s) in foreground window HWND=0x{hwnd.ToInt64():X}.");

                for (int i = 0; i < len; i++)
                {
                    //found.GetElement(i, out IUIAutomationElement item);
                    IUIAutomationElement item = found.GetElement(i);
                    if (item == null)
                    {
                        Debug.WriteLine($"{i + 1}. <null element>");
                        continue;
                    }

                    // Try to get the Name property (UIA_NamePropertyId = 30005) via GetCurrentPropertyValue
                    // If provider doesn't support Name, this will return VT_EMPTY / null.
                    //item.GetCurrentPropertyValue(30005, out object nameObj); // 30005 = UIA_NamePropertyId
                    object nameObj = item.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId); // 30005 = UIA_NamePropertyId
                    string? name = nameObj as string;
                    string automationId = item.CurrentAutomationId.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = "<(no name)>";
                    }

                    Debug.WriteLine($"{i + 1}. {automationId}: {name}");

                    ReleaseIfNotNull(item);
                }

                // Clean up
                ReleaseIfNotNull(found);
                ReleaseIfNotNull(menuItemCond);
                ReleaseIfNotNull(rootElem);
                return len;
            }
            catch (COMException comEx)
            {
                Debug.WriteLine("COM error: " + comEx);
                return Marshal.GetHRForException(comEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled error: " + ex);
                return -16;
            }
            finally
            {
                // Release the top-level automation RCW
                if (raw != null)
                {
                    try
                    {
                        // If we have a real RCW, release it
                        Marshal.FinalReleaseComObject(raw);
                    }
                    catch { }
                }
            }
        }

        // Helper to Release COM objects if present
        private static void ReleaseIfNotNull(object o)
        {
            if (o == null)
            {
                return;
            }

            try
            {
                Marshal.FinalReleaseComObject(o);
            }
            catch { /* ignore */ }
        }

        // #region COM interface declarations (minimal, AOT-friendly)

        // // IUIAutomation (minimal subset we use)
        // [ComImport, Guid("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        // private interface IUIAutomation
        // {
        //     // We define only the methods we call. The vtable order must match the COM definition.
        //     // Full interface has many methods; these signatures must be in the correct order.
        //     // The first 3 methods are IUnknown: QueryInterface, AddRef, Release (implicit)
        //     // Then begin IUIAutomation methods -- we declare placeholders for methods we don't use so the vtable indices align.

        //     // --- IUnknown methods are not declared here (runtime handles them) ---

        //     // The following declarations are ordered to match the vtable. Methods we don't use are declared as reserved stubs.

        //     // 4: GetRootElement (we don't call it) - keep signature to preserve order
        //     int Reserved0();

        //     // 5: ElementFromHandle
        //     [PreserveSig]
        //     int ElementFromHandle(IntPtr hwnd, out IUIAutomationElement element);

        //     // 6: Reserved / other methods
        //     int Reserved1();
        //     int Reserved2();
        //     int Reserved3();
        //     int Reserved4();

        //     // CreatePropertyCondition
        //     [PreserveSig]
        //     int CreatePropertyCondition(int propertyId, [MarshalAs(UnmanagedType.Struct)] object value, out IUIAutomationCondition condition);

        //     // There are many more methods; we do not declare them. The exact vtable order above preserves indices for the methods we call.
        // }

        // // IUIAutomationElement (minimal subset)
        // [ComImport, Guid("D22108AA-8AC5-49A5-837B-37BBB3D7591E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        // private interface IUIAutomationElement
        // {
        //     // Placeholder methods to walk vtable positions; we declare only what we use.
        //     int Reserved0(); // GetRuntimeId etc
        //     int Reserved1();
        //     int Reserved2();
        //     int Reserved3();
        //     int Reserved4();
        //     int Reserved5();

        //     // FindAll (this index must match actual vtable index for FindAll in IUIAutomationElement)
        //     [PreserveSig]
        //     int FindAll(int treeScope, IUIAutomationCondition condition, out IUIAutomationElementArray found);

        //     // GetCurrentPropertyValue
        //     [PreserveSig]
        //     int GetCurrentPropertyValue(int propertyId, [MarshalAs(UnmanagedType.Struct)] out object retVal);

        //     // Many other methods exist but are not declared here.
        // }

        // // IUIAutomationCondition (no members - used as opaque handle)
        // [ComImport, Guid("8C6A20E8-0C6B-4A0A-9F6C-0A55F6D99E9E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        // private interface IUIAutomationCondition
        // {
        //     // Intentionally empty: treated as opaque condition object
        // }

        // // IUIAutomationElementArray (minimal)
        // [ComImport, Guid("14314595-B4BC-11DF-8D0E-001B7C0C9D2B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        // private interface IUIAutomationElementArray
        // {
        //     [PreserveSig]
        //     int get_Length(out int length);

        //     [PreserveSig]
        //     int GetElement(int index, out IUIAutomationElement element);
        // }

        // #endregion
    }
}