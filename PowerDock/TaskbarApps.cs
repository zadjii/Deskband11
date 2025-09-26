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
    public class TaskbarApps
    {
        public static List<TaskbarApp> GetTaskbarWindows()
        {
            List<TaskbarApp> windows = new();

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd) && GetWindow(hWnd, GW_OWNER) == IntPtr.Zero)
                {
                    long exStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();
                    if ((exStyle & WS_EX_TOOLWINDOW) != WS_EX_TOOLWINDOW)
                    {
                        StringBuilder sb = new(1024);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            nint icon = GetWindowIcon(hWnd);
                            IRandomAccessStream? iconStream = icon != IntPtr.Zero ? ConvertIconToStream(icon) : null;
                            // Clean up the unmanaged handle without risking a use-after-free.
                            Windows.Win32.PInvoke.DestroyIcon((Windows.Win32.UI.WindowsAndMessaging.HICON)icon);
                            windows.Add(new TaskbarApp
                            {
                                hWnd = hWnd,
                                Title = title,
                                IconStream = iconStream
                            });
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }


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
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, IntPtr lParam);
        private const uint WM_GETICON = 0x007F;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr", SetLastError = true)]
        private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);
        private const int GCL_HICONSM = -34;
    }

    public class TaskbarApp
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
    }

    public class MainViewModel : IDisposable
    {
        public ObservableCollection<TaskbarApp> Apps { get; } = new();

        private WinEventDelegate? _hookProc;
        private IntPtr _hookHandle = IntPtr.Zero;

        private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public MainViewModel()
        {
            Refresh();

            // install window event hook
            _hookProc = new WinEventDelegate(WinEventCallback);
            _hookHandle = SetWinEventHook(
                EVENT_OBJECT_CREATE,
                EVENT_OBJECT_HIDE,
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

        public void Refresh()
        {
            Apps.Clear();
            List<TaskbarApp> windows = TaskbarApps.GetTaskbarWindows();
            foreach (TaskbarApp app in windows)
            {
                Apps.Add(app);
            }
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject != OBJID_WINDOW || hwnd == IntPtr.Zero)
            {
                return;
            }

            // For simplicity: refresh entire list on any relevant event
            dispatcherQueue.TryEnqueue(() => Refresh());
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

        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        private const int OBJID_WINDOW = 0;
    }
}