using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Microsoft.CmdPal.Common.Helpers;
using Microsoft.CmdPal.UI.Helpers;
using Microsoft.UI.Dispatching;
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
        IRecipient<TaskbarRestartMessage>,
        IRecipient<QuitMessage>
    {
        private readonly uint WM_TASKBAR_RESTART;
        private readonly HWND _hwnd;
        private readonly TrayIconService _trayIconService = new();
        private AppWindow _appWindow;
        private Tasklist _tasklist;

        // Constants for Windows messages related to display changes
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int WM_DESTROY = 0x0002;

        // Store the original WndProc
        private WNDPROC? _originalWndProc;
        private WNDPROC? _hotkeyWndProc;

        // Debouncer to throttle UpdateLayoutForDPI calls
        private readonly DispatcherQueueTimer _updateLayoutDebouncer;
        private readonly DispatcherQueueTimer _updateTaskbarButtonsTimer;

        private double _lastContentSpace = 0;

        public MainWindow()
        {
            InitializeComponent();

            WM_TASKBAR_RESTART = PInvoke.RegisterWindowMessage("TaskbarCreated");

            _hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this).ToInt32());

            // Initialize debouncer with 300ms delay to throttle UpdateLayoutForDPI calls
            _updateLayoutDebouncer = DispatcherQueue.CreateTimer();

            // Timer to re-layout based on available space in taskbar
            _updateTaskbarButtonsTimer = DispatcherQueue.CreateTimer();
            _updateTaskbarButtonsTimer.Tick += (s, e) => ClipWindow(onlyIfButtonsChanged: true);
            _updateTaskbarButtonsTimer.Interval = TimeSpan.FromMilliseconds(500);
            _updateTaskbarButtonsTimer.Start();

            this.VisibilityChanged += MainWindow_VisibilityChanged;
            // this.ItemsBar.SizeChanged += ItemsBar_SizeChanged;
            //this.Root.SizeChanged += ItemsBar_SizeChanged;
            this.MainContent.SizeChanged += ItemsBar_SizeChanged;

            WeakReferenceMessenger.Default.Register<OpenSettingsMessage>(this);
            WeakReferenceMessenger.Default.Register<TaskbarRestartMessage>(this);
            WeakReferenceMessenger.Default.Register<QuitMessage>(this);

            _appWindow = this.AppWindow;

            _tasklist = new Tasklist();

            // Set up custom window procedure to listen for display changes
            // LOAD BEARING: If you don't stick the pointer to HotKeyPrc into a
            // member (and instead like, use a local), then the pointer we marshal
            // into the WindowLongPtr will be useless after we leave this function,
            // and our **WindProc will explode**.
            _hotkeyWndProc = CustomWndProc;
            nint hotKeyPrcPointer = Marshal.GetFunctionPointerForDelegate(_hotkeyWndProc);
            _originalWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(PInvoke.SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, hotKeyPrcPointer));

            ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar?.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            MoveToTaskbar();
            _trayIconService.SetupTrayIcon(true);

        }

        private void ItemsBar_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            Debug.WriteLine($"Content size changed");

            ClipWindow();
        }

        private void MainWindow_VisibilityChanged(object sender, Microsoft.UI.Xaml.WindowVisibilityChangedEventArgs args)
        {
            MoveToTaskbar();
        }
        private LRESULT CustomWndProc(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            // Handle display change messages
            if (uMsg == WM_DISPLAYCHANGE)
            {
                Debug.WriteLine("WM_DISPLAYCHANGE");
                // Use dispatcher to ensure we're on the UI thread
                DispatcherQueue.TryEnqueue(() => MoveToTaskbar());
            }
            else if (uMsg == WM_SETTINGCHANGE)
            {
                if (wParam == (uint)SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETWORKAREA)
                {
                    Debug.WriteLine($"WM_SETTINGCHANGE(SPI_SETWORKAREA)");
                    // Use debounced call to throttle rapid successive calls
                    DispatcherQueue.TryEnqueue(() => TriggerDebouncedLayoutUpdate());
                }
            }
            else if (uMsg == WM_DESTROY)
            {
                // IF WE GOT THIS, WE SHOULD EAT IT
                // BUT WE DON'T
                // Somewhere in the stack, XAML just removes our whole UI tree from the
                // DesktopWindowXamlSource, and we're in oblivion. 
                Debug.WriteLine("WM_DESTROY");
                return (LRESULT)0;
            }
            //else if (uMsg == WM_TASKBAR_RESTART)
            //{
            //    Debug.WriteLine("WM_TASKBAR_RESTART");
            //    DispatcherQueue.TryEnqueue(async () => await UpdateLayoutForDPI());
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
            MainContent.Padding = new Thickness(0);
        }

        private void TriggerDebouncedLayoutUpdate()
        {
            _updateLayoutDebouncer.Debounce(
                () =>
                {
                    UpdateLayoutForDPI();
                },
                interval: TimeSpan.FromMilliseconds(200),
                immediate: false);

        }

        private void MoveToTaskbar()
        {
            if (_appWindow is null)
            {
                Debug.WriteLine("unexpectedly, AppWindow was null");
                return;
            }


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


        private bool UpdateTaskbarButtons()
        {
            _tasklist.Update();
            List<TasklistButton> buttons = _tasklist.GetButtons();
            int maxRight = 0;
            int totalWidth = 0;
            string lastButton = string.Empty;
            foreach (TasklistButton button in buttons)
            {
                totalWidth += button.Width;
                int right = button.X + button.Width;
                if (right > maxRight)
                {
                    maxRight = right;
                    lastButton = button.Name;
                }
            }
            TaskbarButtons.Width = new GridLength(maxRight);

            double available = this.Bounds.Width; // Root.ActualWidth

            int taskbarReserverdInDips = 120 + maxRight;
            double forContent = available - taskbarReserverdInDips - TrayIcons.Width.Value;
            double reservedContent = WindowsLogo.ActualWidth + TaskbarButtons.ActualWidth /*+ Mid.ActualWidth*/ + TrayIcons.ActualWidth;
            double forContent2 = available - reservedContent;

            if (_lastContentSpace == forContent)
            {
                return false;
            }

            Debug.WriteLine($"lastButton: {lastButton}");
            Debug.WriteLine($"maxRight: {maxRight}");
            Debug.WriteLine($"totalWidth: {totalWidth}");
            Debug.WriteLine($"[{available}]");
            Debug.WriteLine($"[{WindowsLogo.ActualWidth}][{TaskbarButtons.ActualWidth}][{Mid.ActualWidth}][content={forContent}][{TrayIcons.ActualWidth}]");
            Debug.WriteLine($"forContent: {forContent}");
            Debug.WriteLine($"{forContent} =? {forContent2}");

            if (forContent > 0)
            {
                ContentColumn.MaxWidth = Root.ActualWidth == 0 ? double.MaxValue : forContent;
                ContentColumn.Width = GridLength.Auto;
            }
            else
            {
                ContentColumn.MaxWidth = 0;
                ContentColumn.Width = new GridLength(0);
            }
            _lastContentSpace = forContent;
            return true;
        }
        private void ClipWindow(bool onlyIfButtonsChanged = false)
        {
            bool taskbarChanged = UpdateTaskbarButtons();
            if (onlyIfButtonsChanged && !taskbarChanged)
            {
                return;
            }

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
        }

        public void Receive(OpenSettingsMessage message)
        {
            // do nothing
        }
        public void Receive(TaskbarRestartMessage message)
        {
            Debug.WriteLine("WM_TASKBAR_RESTART");
            DispatcherQueue.TryEnqueue(() =>
            {
                // I cannot for the life of me get the window to reparent to the new taskbar window. 
                // Maybe I'm just always doing this too fast?
                // I don't know. 
                // Every time we get this, it seems like the content in the ContentAreaPresenter is already null
                // and our ActualWidth is now 0, and the DPI is 0, so we get no size
                // 
                // This is just not getting fixed during a hackathon

                ////this.Hide();
                //PInvoke.SetParent(_hwnd, HWND.Null);
                ////this.Show();
                //await Task.Delay(3000);
                //await UpdateLayoutForDPI();

                //this.Closed -= MainWindow_Closed;
                //_trayIconService.Destroy();
                //MainWindow newWindow = new();
                //newWindow.Activate();
                //WeakReferenceMessenger.Default.UnregisterAll(this); // TODO! not AOT safe

                //this.CoreWindow.Close();

                this.Close();
            });

        }

        public void Receive(QuitMessage message)
        {
            this.VisibilityChanged -= MainWindow_VisibilityChanged;
            this.Root.SizeChanged -= ItemsBar_SizeChanged;

            // Stop the debouncer to prevent any pending calls
            _updateLayoutDebouncer?.Stop();

            DispatcherQueue.TryEnqueue(() => Close());
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _trayIconService.Destroy();
            Environment.Exit(0);
        }
    }

    public record OpenSettingsMessage();
    public record QuitMessage();
    public record TaskbarRestartMessage();
}
