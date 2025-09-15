using Microsoft.UI.Windowing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DeskBand11
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        private readonly HWND _hwnd;

        public MainWindow()
        {
            InitializeComponent();
            _hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this).ToInt32());
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            MoveToTaskbar();
        }

        private void MoveToTaskbar()
        {
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

            RECT reBarRect = new();
            PInvoke.GetWindowRect(reBarWindow, out reBarRect);

            PInvoke.SetWindowPos(thisWindow,
                         HWND.Null,
                         taskbarRect.left,
                         reBarRect.top - taskbarRect.top,
                         taskbarRect.right - taskbarRect.left,
                         reBarRect.bottom - reBarRect.top,
                         0);

            //HostControl.Width = Height;
        }
    }
}
