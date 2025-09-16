using CommunityToolkit.Mvvm.Messaging;
using Microsoft.CmdPal.UI.Helpers;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DeskBand11
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx,
        IRecipient<OpenSettingsMessage>,
        IRecipient<QuitMessage>
    {
        private readonly uint WM_TASKBAR_RESTART;
        private readonly uint UxdDisplayChangeMessage;
        private readonly uint HotplugDetected;
        private readonly HWND _hwnd;
        private readonly TrayIconService _trayIconService = new();
        private AppWindow _appWindow;

        // Constants for Windows messages related to display changes
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int WM_SETTINGCHANGE = 0x001A;

        // Store the original WndProc
        private WNDPROC? _originalWndProc;
        private WNDPROC? _hotkeyWndProc;

        public MainWindow()
        {
            InitializeComponent();

            WM_TASKBAR_RESTART = PInvoke.RegisterWindowMessage("TaskbarCreated");
            UxdDisplayChangeMessage = PInvoke.RegisterWindowMessage("UxdDisplayChangeMessage");
            HotplugDetected = PInvoke.RegisterWindowMessage("HotplugDetected");

            _hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this).ToInt32());

            this.VisibilityChanged += MainWindow_VisibilityChanged;
            // this.ItemsBar.SizeChanged += ItemsBar_SizeChanged;
            this.Root.SizeChanged += ItemsBar_SizeChanged;

            WeakReferenceMessenger.Default.Register<OpenSettingsMessage>(this);
            WeakReferenceMessenger.Default.Register<QuitMessage>(this);

            _appWindow = this.AppWindow;

            // Set up custom window procedure to listen for display changes

            // LOAD BEARING: If you don't stick the pointer to HotKeyPrc into a
            // member (and instead like, use a local), then the pointer we marshal
            // into the WindowLongPtr will be useless after we leave this function,
            // and our **WindProc will explode**.
            _hotkeyWndProc = CustomWndProc;
            nint hotKeyPrcPointer = Marshal.GetFunctionPointerForDelegate(_hotkeyWndProc);
            _originalWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(PInvoke.SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, hotKeyPrcPointer));

            MoveToTaskbar();
            _trayIconService.SetupTrayIcon(true);

        }

        private void ItemsBar_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            ClipWindow();
        }

        private void MainWindow_VisibilityChanged(object sender, Microsoft.UI.Xaml.WindowVisibilityChangedEventArgs args)
        {
            MoveToTaskbar();
        }

        //private void SetupDisplayChangeListener()
        //{
        //    // Get the current window procedure
        //    nint currentWndProc = PInvoke.GetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC);

        //    // Create our custom window procedure delegate
        //    _originalWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(currentWndProc);

        //    // Set our custom window procedure
        //    nint newWndProcPtr = Marshal.GetFunctionPointerForDelegate<WNDPROC>(CustomWndProc);
        //    PInvoke.SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, newWndProcPtr);
        //}

        private LRESULT CustomWndProc(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            // Handle display change messages
            if (uMsg == WM_DISPLAYCHANGE)
            {
                Debug.WriteLine("WM_DISPLAYCHANGE");
                // Use dispatcher to ensure we're on the UI thread
                DispatcherQueue.TryEnqueue(() => MoveToTaskbar());
            }
            //else if (uMsg == WM_SETTINGCHANGE)
            //{
            //    // Check if this is a display-related setting change
            //    string? settingName = Marshal.PtrToStringUni(lParam);
            //    if (settingName == "intl" || settingName == null)
            //    {
            //        // This might be a display-related change
            //        Debug.WriteLine($"WM_SETTINGCHANGE({settingName})");
            //        _ = Task.Delay(1000).ContinueWith((t) => DispatcherQueue.TryEnqueue(() => MoveToTaskbar()));

            //    }
            //}
            else if (uMsg == WM_SETTINGCHANGE)
            {
                if (wParam == (uint)SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETWORKAREA)
                {
                    Debug.WriteLine($"WM_SETTINGCHANGE(SPI_SETWORKAREA)");
                    _ = Task.Delay(1000).ContinueWith((t) => DispatcherQueue.TryEnqueue(() => UpdateLayoutForDPI()));

                }
            }
            //else if (uMsg == WM_TASKBAR_RESTART)
            //{

            //    Debug.WriteLine("WM_TASKBAR_RESTART");
            //    DispatcherQueue.TryEnqueue(() => UpdateLayoutForDPI());
            //}
            //else if (uMsg == UxdDisplayChangeMessage)
            //{

            //    Debug.WriteLine("UxdDisplayChangeMessage");
            //    DispatcherQueue.TryEnqueue(() => UpdateLayoutForDPI());
            //}
            //else if (uMsg == HotplugDetected)
            //{

            //    Debug.WriteLine("HotplugDetected");
            //    DispatcherQueue.TryEnqueue(() => UpdateLayoutForDPI());
            //}

            // Call the original window procedure for all messages
            return PInvoke.CallWindowProc(_originalWndProc, hwnd, uMsg, wParam, lParam);
        }
        private async Task UpdateLayoutForDPI()
        {
            MoveToTaskbar();

            await Task.Delay(200);
            MainContent.Padding = new Thickness(1);
            await Task.Delay(10);
            //await Task.Yield();
            MainContent.Padding = new Thickness(0);
        }

        private void MoveToTaskbar()
        {
            if (_appWindow is null)
            {
                Debug.WriteLine("unexpectedly, AppWindow was null");
                return;
            }

            ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

            HWND thisWindow = _hwnd;

            HWND taskbarWindow = PInvoke.FindWindow("Shell_TrayWnd", null);
            HWND reBarWindow = PInvoke.FindWindowEx(taskbarWindow, HWND.Null, "ReBarWindow32", null);

            WINDOW_STYLE oldStyle = (WINDOW_STYLE)PInvoke.GetWindowLong(thisWindow, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            WINDOW_STYLE oldStyleButNotPopup = oldStyle & (~WINDOW_STYLE.WS_POPUP);
            WINDOW_STYLE nowAddChild = oldStyleButNotPopup | WINDOW_STYLE.WS_CHILD;

            PInvoke.SetWindowLong(thisWindow, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)nowAddChild);
            PInvoke.SetParent(thisWindow, taskbarWindow);

            RECT taskbarRect = new();
            PInvoke.GetWindowRect(taskbarWindow, out taskbarRect);
            Debug.WriteLine($"taskbarRect: ({taskbarRect.X}, {taskbarRect.Y}), {taskbarRect.Size}");

            RECT reBarRect = new();
            PInvoke.GetWindowRect(reBarWindow, out reBarRect);

            RECT newWindowRect = new();
            newWindowRect.left = taskbarRect.left;
            newWindowRect.top = reBarRect.top - taskbarRect.top;
            newWindowRect.right = newWindowRect.left + (taskbarRect.right - taskbarRect.left);
            newWindowRect.bottom = newWindowRect.top + (reBarRect.bottom - reBarRect.top);
            Debug.WriteLine($"newWindowRect: ({newWindowRect.left}, {newWindowRect.top}), ({newWindowRect.right}, {newWindowRect.bottom})");

            PInvoke.SetWindowRgn(_hwnd, HRGN.Null, true);

            PInvoke.SetWindowPos(thisWindow,
                         HWND.Null,
                         newWindowRect.left,
                         newWindowRect.top,
                         newWindowRect.Width,
                         newWindowRect.Height,
                         SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

            ClipWindow();
        }

        private void ClipWindow()
        {
            FrameworkElement clipToElement = MainContent;
            System.Numerics.Vector2 clipToSize = clipToElement.ActualSize;
            Windows.Foundation.Point position = clipToElement.TransformToVisual(this.Content).TransformPoint(new());
            float scaleFactor = (float)this.GetDpiForWindow() / 96.0f;
            RECT scaledBounds = new()
            {
                left = (int)(position.X * scaleFactor),
                top = (int)(position.Y * scaleFactor),
                right = (int)((position.X + clipToElement.ActualWidth) * scaleFactor),
                bottom = (int)((position.Y + clipToElement.ActualHeight) * scaleFactor)
            };

            Debug.WriteLine($"ActualWidth: {clipToElement.ActualWidth}");
            Debug.WriteLine($"scaledBounds.Width: {scaledBounds.Width} ({scaledBounds.Width / scaleFactor})");

            Windows.Win32.Graphics.Gdi.HRGN hrgn = PInvoke.CreateRectRgn(scaledBounds.left,
                    scaledBounds.top, scaledBounds.right, scaledBounds.bottom);
            PInvoke.SetWindowRgn(_hwnd, hrgn, true);
            //PInvoke.DeleteObject(hrgn);

            //PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_HIDE);
            //PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWNA);
            //PInvoke.BringWindowToTop(_hwnd);
            //if (_appWindow.Presenter is OverlappedPresenter p)
            //{
            //    p.IsAlwaysOnTop = false;
            //    p.IsAlwaysOnTop = true;
            //}
        }

        public void Receive(OpenSettingsMessage message)
        {
            // do nothing
        }

        public void Receive(QuitMessage message)
        {
            this.VisibilityChanged -= MainWindow_VisibilityChanged;
            this.Root.SizeChanged -= ItemsBar_SizeChanged;

            DispatcherQueue.TryEnqueue(() => Close());
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            // // Restore original window procedure if we have one
            // if (_originalWndProc != null)
            // {
            //     var originalWndProcPtr = Marshal.GetFunctionPointerForDelegate(_originalWndProc);
            //     PInvoke.SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, originalWndProcPtr);
            // }

            _trayIconService.Destroy();
            Environment.Exit(0);
        }
    }

    public record OpenSettingsMessage();
    public record QuitMessage();
}
