using CommunityToolkit.Mvvm.Messaging;
using DeskBand.ViewModels.Messages;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using WinRT.Interop;
using WinUIEx;

namespace PowerDock
{
    public sealed partial class DockWindow : WindowEx, IRecipient<OpenSettingsMessage>
    {
        private readonly Settings _settings;
        private HWND _hwnd = HWND.Null;
        private APPBARDATA _appBarData;
        private uint _callbackMessageId;
        private MainViewModel ViewModel;
        private DockControl _dock;
        private DesktopAcrylicController _acrylicController;
        private SystemBackdropConfiguration _configurationSource;
        // Store the original WndProc
        private WNDPROC? _originalWndProc;
        private WNDPROC? _customWndProc;


        /// <summary>
        /// Gets the current settings instance
        /// </summary>
        internal Settings CurrentSettings => _settings;

        public DockWindow()
        {
            _settings = DockSettingsWindow.LoadUserSettings();

            ViewModel = new MainViewModel(_settings);
            _dock = new DockControl(ViewModel);

            InitializeComponent();
            Root.Children.Add(_dock);
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            AppWindow.IsShownInSwitchers = false;
            if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                overlappedPresenter.SetBorderAndTitleBar(false, false);
                overlappedPresenter.IsResizable = false;
            }
            this.Activated += MainWindow_Activated;
            this.Closed += DockWindow_Closed;
            WeakReferenceMessenger.Default.Register<OpenSettingsMessage>(this);

            _hwnd = GetWindowHandle(this);
            // Subclass the window to intercept messages
            //
            // Set up custom window procedure to listen for display changes
            // LOAD BEARING: If you don't stick the pointer to HotKeyPrc into a
            // member (and instead like, use a local), then the pointer we marshal
            // into the WindowLongPtr will be useless after we leave this function,
            // and our **WindProc will explode**.
            _customWndProc = CustomWndProc;

            _callbackMessageId = PInvoke.RegisterWindowMessage("AppBarMessage");

            nint hotKeyPrcPointer = Marshal.GetFunctionPointerForDelegate(_customWndProc);
            _originalWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(PInvoke.SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, hotKeyPrcPointer));

            //// Load settings asynchronously
            //_ = LoadSettings();


            // Disable minimize and maximize box
            uint style = (uint)PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            style &= ~WS_MINIMIZEBOX; // Remove WS_MINIMIZEBOX
            style &= ~WS_MAXIMIZEBOX; // Remove WS_MAXIMIZEBOX
            PInvoke.SetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)style);


            UpdateSettings();
        }

        private void LoadSettings()
        {
            try
            {
                //Settings loadedSettings = await DockSettingsWindow.LoadUserSettingsAsync();

                //// Update our settings reference
                //_settings = loadedSettings;

                // Update the ViewModel with the loaded settings
                ViewModel.UpdateSettings();

                // If the window handle is available, update the window position
                if (_hwnd != HWND.Null)
                {
                    UpdateSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                // Continue with default settings if loading fails
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_hwnd == HWND.Null)
            {


            }

            // These are used for removing the very subtle shadow/border that we get from Windows 11
            HwndExtensions.ToggleWindowStyle(_hwnd, false, WindowStyle.TiledWindow);
            unsafe
            {
                BOOL value = false;
                PInvoke.DwmSetWindowAttribute(_hwnd, Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, &value, (uint)sizeof(BOOL));
            }
        }

        private void DockWindow_Closed(object sender, WindowEventArgs args)
        {
            // Restore original window procedure if we subclassed it
            //if (_hwnd != HWND.Null && _originalWndProc is not null)
            //{
            //    PInvoke.SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, (nint)_originalWndProc);
            //    _originalWndProc = null;
            //}

            // Clean up app bar
            if (_appBarData.hWnd != IntPtr.Zero)
            {
                DestroyAppBar(_hwnd);
            }
        }

        private HWND GetWindowHandle(Window window)
        {
            nint hwnd = WindowNative.GetWindowHandle(window);
            return new HWND(hwnd);
        }

        private void UpdateSettings()
        {
            SystemBackdrop = SettingsToViews.GetSystemBackdrop(_settings.Backdrop);

            // If the backdrop is acrylic, things are more complicated
            if (_settings.Backdrop == DockBackdrop.Acrylic)
            {
                SetAcrylic();
            }

            _dock.UpdateSettings(_settings);
            uint side = SettingsToViews.GetAppBarEdge(_settings.Side);

            if (_appBarData.hWnd != IntPtr.Zero)
            {
                if (_appBarData.uEdge == side)
                {
                    return;
                }

                DestroyAppBar(_hwnd);
            }
            CreateAppBar(_hwnd);
        }

        // We want to use DesktopAcrylicKind.Thin and custom colors as this is the default material
        // other Shell surfaces are using, this cannot be set in XAML however.
        private void SetAcrylic()
        {
            if (DesktopAcrylicController.IsSupported())
            {
                // Hooking up the policy object.
                _configurationSource = new SystemBackdropConfiguration
                {
                    // Initial configuration state.
                    IsInputActive = true,
                };
                UpdateAcrylic();
            }
        }

        private void UpdateAcrylic()
        {
            if (_acrylicController != null)
            {
                _acrylicController.RemoveAllSystemBackdropTargets();
                _acrylicController.Dispose();
            }

            _acrylicController = GetAcrylicConfig(Content);

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            _acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            _acrylicController.SetSystemBackdropConfiguration(_configurationSource);
        }

        private static DesktopAcrylicController GetAcrylicConfig(UIElement content)
        {
            FrameworkElement? feContent = content as FrameworkElement;

            return feContent?.ActualTheme == ElementTheme.Light
                ? new DesktopAcrylicController()
                {
                    Kind = DesktopAcrylicKind.Thin,
                    TintColor = Color.FromArgb(255, 243, 243, 243),
                    LuminosityOpacity = 0.90f,
                    TintOpacity = 0.0f,
                    FallbackColor = Color.FromArgb(255, 238, 238, 238),
                }
                : new DesktopAcrylicController()
                {
                    Kind = DesktopAcrylicKind.Thin,
                    TintColor = Color.FromArgb(255, 32, 32, 32),
                    LuminosityOpacity = 0.96f,
                    TintOpacity = 0.5f,
                    FallbackColor = Color.FromArgb(255, 28, 28, 28),
                };
        }


        private void CreateAppBar(HWND hwnd)
        {
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

        private void DestroyAppBar(HWND hwnd)
        {

            PInvoke.SHAppBarMessage(ABM_REMOVE, ref _appBarData);
            _appBarData = default;
        }

        private void UpdateWindowPosition()
        {
            uint dpi = PInvoke.GetDpiForWindow(_hwnd);

            int screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);

            // Get system border metrics
            int borderWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXBORDER);
            int edgeWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXEDGE);
            int frameWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXFRAME);

            UpdateAppBarDataForEdge(_settings.Side, _settings.DockSize, dpi / 96.0);

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

        private void UpdateAppBarDataForEdge(Side side, DockSize size, double scaleFactor)
        {
            double horizontalHeightDips = SettingsToViews.HeightForSize(size);
            double verticalWidthDips = SettingsToViews.WidthForSize(size);
            int screenHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
            int screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);

            if (side == Side.Top)
            {
                _appBarData.uEdge = ABE_TOP;
                _appBarData.rc.left = 0;
                _appBarData.rc.top = 0;
                _appBarData.rc.right = screenWidth;
                _appBarData.rc.bottom = (int)(horizontalHeightDips * scaleFactor);
            }
            else if (side == Side.Bottom)
            {
                int heightPixels = (int)(horizontalHeightDips * scaleFactor);

                _appBarData.uEdge = ABE_BOTTOM;
                _appBarData.rc.left = 0;
                _appBarData.rc.top = screenHeight - heightPixels;
                _appBarData.rc.right = screenWidth;
                _appBarData.rc.bottom = screenHeight;
            }
            else if (side == Side.Left)
            {
                int widthPixels = (int)(verticalWidthDips * scaleFactor);

                _appBarData.uEdge = ABE_LEFT;
                _appBarData.rc.left = 0;
                _appBarData.rc.top = 0;
                _appBarData.rc.right = widthPixels;
                _appBarData.rc.bottom = screenHeight;
            }
            else if (side == Side.Right)
            {
                int widthPixels = (int)(verticalWidthDips * scaleFactor);

                _appBarData.uEdge = ABE_RIGHT;
                _appBarData.rc.left = screenWidth - widthPixels;
                _appBarData.rc.top = 0;
                _appBarData.rc.right = screenWidth;
                _appBarData.rc.bottom = screenHeight;
            }
            else
            {
                return;
            }
        }

        private LRESULT CustomWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            // if it's a WM_ACTIVATEAPP, then send us to topmost
            if (msg == WM_ACTIVATEAPP)
            {
                PInvoke.SetWindowPos(hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
            }


            // Intercept WM_SYSCOMMAND to prevent minimize and maximize
            if (msg == WM_SYSCOMMAND)
            {
                int command = (int)(wParam.Value & 0xFFF0);
                if (command == SC_MINIMIZE || command == SC_MAXIMIZE)
                {
                    // Block minimize and maximize commands
                    return new LRESULT(0);
                }
            }

            // Stop min/max on WM_WINDOWPOSCHANGING too
            if (msg == WM_WINDOWPOSCHANGING)
            {
                unsafe
                {
                    WINDOWPOS* pWindowPos = (WINDOWPOS*)lParam.Value;

                    // Check if the window is being hidden (minimized) or if flags suggest minimize/maximize
                    if ((pWindowPos->flags & SWP_HIDEWINDOW) != 0)
                    {
                        // Prevent hiding the window (minimize)
                        pWindowPos->flags &= ~SWP_HIDEWINDOW;
                        pWindowPos->flags |= SWP_SHOWWINDOW;
                    }

                    // Additional check: if the window position suggests it's being minimized or maximized
                    // by checking for dramatic size changes
                    if (pWindowPos->cx <= 0 || pWindowPos->cy <= 0)
                    {
                        // Prevent zero or negative size changes (minimize)
                        pWindowPos->flags |= SWP_NOSIZE;
                    }
                }
            }

            // Handle WM_SIZE to prevent minimize/maximize state changes
            if (msg == WM_SIZE)
            {
                int sizeType = (int)wParam.Value;
                if (sizeType == SIZE_MINIMIZED || sizeType == SIZE_MAXIMIZED)
                {
                    // Block the size change by not calling the original window procedure
                    return new LRESULT(0);
                }
            }

            // Handle WM_SHOWWINDOW to prevent hiding (minimize)
            if (msg == WM_SHOWWINDOW)
            {
                bool isBeingShown = wParam.Value != 0;
                if (!isBeingShown)
                {
                    // Prevent hiding the window
                    return new LRESULT(0);
                }
            }

            // Handle double-click on title bar (non-client area)
            if (msg == WM_NCLBUTTONDBLCLK)
            {
                int hitTest = (int)wParam.Value;
                if (hitTest == HTCAPTION)
                {
                    // Block double-click on title bar to prevent maximize
                    return new LRESULT(0);
                }
            }

            //// Handle keyboard shortcuts that could minimize/maximize
            //if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            //{
            //    int vkCode = (int)wParam.Value;

            //    // Check for Windows key combinations that minimize/maximize
            //    if (PInvoke.GetKeyState(0x5B) < 0) // Left Windows key is pressed
            //    {
            //        if (vkCode == VK_DOWN || vkCode == VK_UP || vkCode == VK_F9)
            //        {
            //            // Block Windows+Down (minimize), Windows+Up (maximize), Windows+F9 (minimize)
            //            return new LRESULT(0);
            //        }
            //    }

            //    if (PInvoke.GetKeyState(0x5C) < 0) // Right Windows key is pressed
            //    {
            //        if (vkCode == VK_DOWN || vkCode == VK_UP || vkCode == VK_F9)
            //        {
            //            // Block Windows+Down (minimize), Windows+Up (maximize), Windows+F9 (minimize)
            //            return new LRESULT(0);
            //        }
            //    }
            //}

            // Handle WM_GETMINMAXINFO to control window size limits
            if (msg == WM_GETMINMAXINFO)
            {
                // We can modify the min/max tracking info here if needed
                // For now, let it pass through but we could restrict max size
            }



            //if (msg == _callbackMessageId)
            //{
            //    if (wParam.Value == ABN_POSCHANGED)
            //    {
            //        UpdateWindowPosition();
            //    }
            //}



            // Call the original window procedure for all other messages
            return PInvoke.CallWindowProc(_originalWndProc, hwnd, msg, wParam, lParam);
        }

        public void Receive(OpenSettingsMessage message)
        {
            // Create and show the settings window
            DockSettingsWindow settingsWindow = new(this, _settings);
            settingsWindow.Activate();
        }

        public void RefreshSettings()
        {
            UpdateSettings();
        }

        private static readonly uint ABM_NEW = 0x0;
        private static readonly uint ABM_REMOVE = 0x1;
        private static readonly uint ABM_QUERYPOS = 0x2;
        private static readonly uint ABM_SETPOS = 0x3;
        private static readonly uint ABM_GETSTATE = 0x4;

        public static readonly uint ABE_LEFT = 0x0;
        public static readonly uint ABE_TOP = 0x1;
        public static readonly uint ABE_RIGHT = 0x2;
        public static readonly uint ABE_BOTTOM = 0x3;

        // Window message constants
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_SIZE = 0x0005;
        private const int WM_SHOWWINDOW = 0x0018;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_NCLBUTTONDBLCLK = 0x00A3;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_ACTIVATE = 0x0006;
        private const int WM_ACTIVATEAPP = 0x001C;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_RESTORE = 0xF120;

        // Window style constants
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;

        // ShowWindow constants
        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;

        // WM_SIZE constants
        private const int SIZE_RESTORED = 0;
        private const int SIZE_MINIMIZED = 1;
        private const int SIZE_MAXIMIZED = 2;
        private const int SIZE_MAXSHOW = 3;
        private const int SIZE_MAXHIDE = 4;

        // Virtual key constants
        private const int VK_F9 = 0x78;   // F9 key (for Windows+F9 minimize)
        private const int VK_DOWN = 0x28; // Down arrow (for Windows+Down minimize)
        private const int VK_UP = 0x26;   // Up arrow (for Windows+Up maximize)

        // Title bar hit test constants
        private const int HTCAPTION = 2;

        // WINDOWPOS flags
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOREDRAW = 0x0008;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_HIDEWINDOW = 0x0080;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WINDOWPOS
    {
        public HWND hwnd;
        public HWND hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }

    public enum Side
    {
        Left = 0,
        Top = 1,
        Right = 2,
        Bottom = 3,
    }

    public enum DockSize
    {
        Small,
        Medium,
        Large
    }

    public enum DockBackdrop
    {
        Mica,
        Transparent,
        Acrylic
    }

    public class Settings
    {
        public bool ShowAppTitles { get; set; } = false;
        public bool ShowSearchButton { get; set; } = true;
        public Side Side { get; set; } = Side.Top;
        public DockSize DockSize { get; set; } = DockSize.Small;
        public DockBackdrop Backdrop { get; set; } = DockBackdrop.Acrylic;
    }

    internal static class SettingsToViews
    {
        public static double WidthForSize(DockSize size)
        {
            return size switch
            {
                DockSize.Small => 128,
                DockSize.Medium => 192,
                DockSize.Large => 256,
                _ => throw new NotImplementedException(),
            };
        }
        public static double HeightForSize(DockSize size)
        {
            return size switch
            {
                DockSize.Small => 32,
                DockSize.Medium => 54,
                DockSize.Large => 76,
                _ => throw new NotImplementedException(),
            };
        }
        public static Microsoft.UI.Xaml.Media.SystemBackdrop? GetSystemBackdrop(DockBackdrop backdrop)
        {
            return backdrop switch
            {
                DockBackdrop.Mica => new MicaBackdrop(),
                DockBackdrop.Transparent => new TransparentTintBackdrop(),
                DockBackdrop.Acrylic => null, // new DesktopAcrylicBackdrop(),
                _ => throw new NotImplementedException(),
            };
        }

        public static uint GetAppBarEdge(Side side)
        {
            return side switch
            {
                Side.Left => DockWindow.ABE_LEFT,
                Side.Top => DockWindow.ABE_TOP,
                Side.Right => DockWindow.ABE_RIGHT,
                Side.Bottom => DockWindow.ABE_BOTTOM,
                _ => throw new NotImplementedException(),
            };
        }
    }

}
