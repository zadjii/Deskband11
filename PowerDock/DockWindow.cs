using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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
        //ObservableCollection<TaskbarApp> TaskbarItems { get; set; } = new ObservableCollection<TaskbarApp>();

        private HWND _hwnd = HWND.Null;
        private APPBARDATA _appBarData;
        private uint _callbackMessageId;
        private MainViewModel ViewModel;

        public DockWindow()
        {
            ViewModel = new MainViewModel();

            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            AppWindow.IsShownInSwitchers = false;
            if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                overlappedPresenter.SetBorderAndTitleBar(false, false);
                overlappedPresenter.IsResizable = false;
            }
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

            _appBarData = new APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd,
                uCallbackMessage = _callbackMessageId
            };

            // Register this window as an appbar
            PInvoke.SHAppBarMessage(ABM_NEW, ref _appBarData);

            UpdateWindowPosition();
        }

        private void UpdateWindowPosition()
        {
            var heightDips = ButtonsRowDef.Height.Value; // height of your bar

            var dpi = PInvoke.GetDpiForWindow(_hwnd);

            int heightPixels = (int)(heightDips * dpi / 96); // convert to physical pixels

            int screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);

            // Get system border metrics
            int borderWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXBORDER);
            int edgeWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXEDGE);
            int frameWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXFRAME);

            _appBarData.uEdge = ABE_TOP;
            _appBarData.rc.left = 0;
            _appBarData.rc.top = 0;
            _appBarData.rc.right = screenWidth;
            _appBarData.rc.bottom = heightPixels;

            // Query and set position
            PInvoke.SHAppBarMessage(ABM_QUERYPOS, ref _appBarData);
            PInvoke.SHAppBarMessage(ABM_SETPOS, ref _appBarData);

            // Account for system borders when moving the window
            // Adjust position to account for window frame/border
            int adjustedLeft = _appBarData.rc.left - frameWidth;
            int adjustedTop = _appBarData.rc.top - frameWidth;
            int adjustedWidth = (_appBarData.rc.right - _appBarData.rc.left) + (2 * frameWidth);
            int adjustedHeight = (_appBarData.rc.bottom - _appBarData.rc.top) + (2 * frameWidth);

            // Move the actual window
            PInvoke.MoveWindow(_hwnd,
                adjustedLeft,
                adjustedTop,
                adjustedWidth,
                adjustedHeight,
                 true);
        }

        private static readonly uint ABM_NEW = 0x0;
        private static readonly uint ABM_REMOVE = 0x1;
        private static readonly uint ABM_QUERYPOS = 0x2;
        private static readonly uint ABM_SETPOS = 0x3;
        private static readonly uint ABM_GETSTATE = 0x4;

        private static readonly uint ABE_TOP = 0x1;
    }
}
