using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PowerDock
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DockWindow : Window
    {
        ObservableCollection<TaskbarApp> TaskbarItems { get; set; } = new ObservableCollection<TaskbarApp>();

        private HWND _hwnd = HWND.Null;
        private uint _callbackMessageId;

        public DockWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            AppWindow.IsShownInSwitchers = false;
            this.Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_hwnd == HWND.Null)
            {
                _hwnd = GetWindowHandle(this);
                RegisterAppBar(_hwnd);
            }
        }
        private HWND GetWindowHandle(Window window)
        {
            nint hwnd = WindowNative.GetWindowHandle(window);
            return new HWND(hwnd);
        }
        private void RegisterAppBar(HWND hwnd)
        {
            _callbackMessageId = PInvoke.RegisterWindowMessage("AppBarMessage");

            APPBARDATA abd = new()
            {
                cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd,
                uCallbackMessage = _callbackMessageId
            };

            // Register this window as an appbar
            PInvoke.SHAppBarMessage(ABM_NEW, ref abd);

            int height = 32; // height of your bar

            int screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);

            abd.uEdge = ABE_TOP;
            abd.rc.left = 0;
            abd.rc.top = 0;
            abd.rc.right = screenWidth;
            abd.rc.bottom = height;

            // Query and set position
            PInvoke.SHAppBarMessage(ABM_QUERYPOS, ref abd);
            PInvoke.SHAppBarMessage(ABM_SETPOS, ref abd);

            // Move the actual window
            PInvoke.MoveWindow(hwnd,
                abd.rc.left,
                abd.rc.top,
                abd.rc.right - abd.rc.left,
                abd.rc.bottom - abd.rc.top,
                true);
        }
        private static readonly uint ABM_NEW = 0x0;
        private static readonly uint ABM_REMOVE = 0x1;
        private static readonly uint ABM_QUERYPOS = 0x2;
        private static readonly uint ABM_SETPOS = 0x3;
        private static readonly uint ABM_GETSTATE = 0x4;

        private static readonly uint ABE_TOP = 0x1;
        //protected override void OnClosed(WindowEventArgs args)
        //{
        //    // Unregister appbar when closed
        //    APPBARDATA abd = new()
        //    {
        //        cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
        //        hWnd = _hWnd
        //    };
        //    PInvoke.SHAppBarMessage(ABM_REMOVE, ref abd);

        //    base.OnClosed(args);
        //}

        [RelayCommand]
        void PressButton()
        {
            System.Collections.Generic.List<TaskbarApp> items = TaskbarApps.GetTaskbarWindows();
            TaskbarItems.Clear();
            foreach (TaskbarApp item in items)
            {
                TaskbarItems.Add(item);
            }
        }

        private void Button_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            PressButton();
        }
    }
}
