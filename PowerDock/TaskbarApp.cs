using CommunityToolkit.Mvvm.Input;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Runtime.InteropServices;
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
}