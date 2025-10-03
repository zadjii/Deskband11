using CommunityToolkit.Mvvm.Input;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Storage.Streams;

namespace PowerDock
{
    public partial class TaskbarApp
    {
        public IntPtr hWnd;
        public required string Title;
        public IRandomAccessStream? IconStream;
        public IconInfo Icon
        {
            get
            {
                return IconStream is null ? new(string.Empty) : IconInfo.FromStream(IconStream);
            }
        }

        [RelayCommand]
        public void SwitchTo()
        {
            if (hWnd == IntPtr.Zero)
            {
                return;
            }
            WINDOWPLACEMENT placement = new();
            placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            if (GetWindowPlacement(hWnd, ref placement))
            {
                // Only restore if minimized
                if (placement.showCmd == SW_SHOWMINIMIZED)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                }
            }

            // Bring to foreground
            SetForegroundWindow(hWnd);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        private const int SW_RESTORE = 9;
        private const int SW_SHOWMINIMIZED = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }
    }

    public class MainViewModel : IDisposable
    {
        public ObservableCollection<TaskbarApp> Apps { get; } = new();
        private readonly Dictionary<IntPtr, TaskbarApp> _map = new();

        private WinEventDelegate? _hookProc;
        private IntPtr _hookHandle = IntPtr.Zero;

        private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public MainViewModel()
        {
            RefreshAll(); // initial load

            // install window event hook
            _hookProc = new WinEventDelegate(WinEventCallback);
            _hookHandle = SetWinEventHook(
                EVENT_OBJECT_CREATE,
                EVENT_OBJECT_NAMECHANGE, // include name/title changes
                IntPtr.Zero,
                _hookProc,
                0, 0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        }

        public void Dispose()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }

        private void RefreshAll()
        {
            Apps.Clear();
            _map.Clear();

            foreach (TaskbarApp app in EnumerateTaskbarWindows())
            {
                Apps.Add(app);
                _map[app.hWnd] = app;
            }
        }

        private static TaskbarApp[] EnumerateTaskbarWindows()
        {
            List<TaskbarApp> windows = new();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsTaskbarCandidate(hWnd, out TaskbarApp? app))
                {
                    windows.Add(app);
                }

                return true;
            }, IntPtr.Zero);
            return windows.ToArray();
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject != OBJID_WINDOW || hwnd == IntPtr.Zero)
            {
                return;
            }

            dispatcherQueue.TryEnqueue(() =>
            {
                switch (eventType)
                {
                    case EVENT_OBJECT_CREATE:
                    case EVENT_OBJECT_SHOW:
                        TryAddWindow(hwnd);
                        break;

                    case EVENT_OBJECT_DESTROY:
                    case EVENT_OBJECT_HIDE:
                        TryRemoveWindow(hwnd);
                        break;

                    case EVENT_OBJECT_NAMECHANGE:
                        TryUpdateWindow(hwnd);
                        break;
                }
            });
        }

        private void TryAddWindow(IntPtr hwnd)
        {
            if (_map.ContainsKey(hwnd))
            {
                return;
            }

            if (IsTaskbarCandidate(hwnd, out TaskbarApp? app))
            {
                Apps.Add(app);
                _map[hwnd] = app;
            }
        }

        private void TryRemoveWindow(IntPtr hwnd)
        {
            if (_map.TryGetValue(hwnd, out TaskbarApp? app))
            {
                Apps.Remove(app);
                _map.Remove(hwnd);
            }
        }

        private void TryUpdateWindow(IntPtr hwnd)
        {
            if (_map.TryGetValue(hwnd, out TaskbarApp? app))
            {
                StringBuilder sb = new(1024);
                GetWindowText(hwnd, sb, sb.Capacity);
                string newTitle = sb.ToString();
                if (!string.IsNullOrWhiteSpace(newTitle) && newTitle != app.Title)
                {
                    app.Title = newTitle;
                    // notify UI: remove and reinsert (ObservableCollection doesn't auto-refresh properties)
                    Apps.Remove(app);
                    Apps.Add(app);
                    _map[hwnd] = app;
                }
            }
        }
        private static bool IsTaskbarCandidate(IntPtr hWnd, out TaskbarApp app)
        {
            app = null!;
            //if (!IsWindowVisible(hWnd) || GetWindow(hWnd, GW_OWNER) != IntPtr.Zero)
            //{
            //    return false;
            //}

            //long exStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();
            //if ((exStyle & WS_EX_TOOLWINDOW) == WS_EX_TOOLWINDOW)
            //{
            //    return false;
            //}

            //StringBuilder sb = new(1024);
            //GetWindowText(hWnd, sb, sb.Capacity);
            //string title = sb.ToString();
            //if (string.IsNullOrWhiteSpace(title))
            //{
            //    return false;
            //}
            if (IsTaskbarEligibleWindow(hWnd, out string? title))
            {
                nint hIcon = GetWindowIcon(hWnd);
                IRandomAccessStream? iconStream = hIcon != IntPtr.Zero ? ConvertIconToStream(hIcon) : null;

                app = new TaskbarApp
                {
                    hWnd = hWnd,
                    Title = title,
                    IconStream = iconStream
                };
                return true;

            }
            return false;
        }
        private static bool IsTaskbarEligibleWindow(IntPtr hWnd, out string title)
        {
            title = string.Empty;
            if (!IsWindowVisible(hWnd))
            {
                return false;
            }

            //// Skip cloaked windows (like Windows Input Experience, background XAML hosts)
            //if (IsCloaked(hWnd))
            //{
            //    return false;
            //}

            // Skip windows with owners (tool or child windows)
            if (GetWindow(hWnd, GW_OWNER) != IntPtr.Zero)
            {
                return false;
            }

            // Skip tool windows
            long exStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();
            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
            {
                return false;
            }

            long style = GetWindowLongPtr(hWnd, GWL_STYLE).ToInt64();
            if ((style & WS_POPUP) != 0)
            {
                return false;
            }
            // Must have a title
            StringBuilder sb = new(1024);
            GetWindowText(hWnd, sb, sb.Capacity);
            title = sb.ToString();
            return !string.IsNullOrWhiteSpace(title);
        }

        private static bool IsCloaked(IntPtr hWnd)
        {
            return DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloakedVal, sizeof(int)) == 0 ? cloakedVal != 0 : false;
        }

        // --- WinEvent hooks ---
        private delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess,
            uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_OBJECT_SHOW = 0x8002;
        private const uint EVENT_OBJECT_HIDE = 0x8003;
        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;

        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        private const int OBJID_WINDOW = 0;
        private static IntPtr GetWindowIcon(IntPtr hWnd)
        {
            const int ICON_SMALL2 = 2;

            IntPtr hIcon = SendMessage(hWnd, WM_GETICON, ICON_SMALL2, IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
            {
                hIcon = SendMessage(hWnd, WM_GETICON, ICON_SMALL, IntPtr.Zero);
            }

            if (hIcon == IntPtr.Zero)
            {
                hIcon = SendMessage(hWnd, WM_GETICON, ICON_BIG, IntPtr.Zero);
            }

            if (hIcon == IntPtr.Zero)
            {
                hIcon = GetClassLongPtr(hWnd, GCL_HICONSM);
            }

            return hIcon;
        }

        private static IRandomAccessStream? ConvertIconToStream(IntPtr hIcon)
        {
            using (System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(hIcon))
            using (MemoryStream memoryStream = new())
            {
                icon.ToBitmap().Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;
                byte[] buffer = memoryStream.ToArray();
                InMemoryRandomAccessStream stream = new();
                using IOutputStream outputStream = stream.GetOutputStreamAt(0);
                using (DataWriter dataWriter = new(outputStream))
                {
                    dataWriter.WriteBytes(buffer);
                    dataWriter.StoreAsync().Wait();
                    dataWriter.FlushAsync().Wait();
                }

                return stream;
            }
            return null;
        }

        // P/Invoke

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        private const uint GW_OWNER = 4;

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const long WS_POPUP = 0x80000000L;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, IntPtr lParam);
        private const uint WM_GETICON = 0x007F;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr", SetLastError = true)]
        private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);
        private const int GCL_HICONSM = -34;

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(
    IntPtr hwnd,
    int dwAttribute,
    out int pvAttribute,
    int cbAttribute);

        private const int DWMWA_CLOAKED = 14;
    }
}