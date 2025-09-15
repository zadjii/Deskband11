using JPSoftworks.MediaControlsExtension.Model;
using JPSoftworks.MediaControlsExtension.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public sealed partial class MainWindow : WindowEx, INotifyPropertyChanged
    {
        private readonly HWND _hwnd;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<TaskbarItemViewModel> Bands { get; set; }

        public MainWindow()
        {
            Bands = new();
            Bands.Add(new HelloWorldTaskBand());
            Bands.Add(new AudioBand());

            InitializeComponent();
            _hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this).ToInt32());

            this.VisibilityChanged += MainWindow_VisibilityChanged;
            this.ItemsBar.SizeChanged += ItemsBar_SizeChanged;
        }

        private void ItemsBar_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            ClipWindow();
        }

        private void MainWindow_VisibilityChanged(object sender, Microsoft.UI.Xaml.WindowVisibilityChangedEventArgs args)
        {
            MoveToTaskbar();
        }

        private void MoveToTaskbar()
        {
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

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
            //this.Height = reBarRect.bottom - reBarRect.top;
            ClipWindow();
        }

        private void ClipWindow()
        {
            // get the size of the ItemsBar
            // Set the clip region of this window to that size
            Microsoft.UI.Xaml.Controls.ItemsControl clipToElement = ItemsBar;
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
            //RECT windowRect = new();
            //PInvoke.GetWindowRect(_hwnd, out windowRect);
            //scaledBounds.left += windowRect.left;
            //scaledBounds.right += windowRect.left;
            //scaledBounds.top += windowRect.top;
            //scaledBounds.bottom += windowRect.top;

            PInvoke.SetWindowRgn(_hwnd,
                PInvoke.CreateRectRgn(scaledBounds.left,
                    scaledBounds.top, scaledBounds.right, scaledBounds.bottom),
                    true);
        }
    }

    public partial class HelloWorldTaskBand : TaskbarItemViewModel
    {
        public HelloWorldTaskBand()
        {
            Title = "Hello world";
        }
    }

    public partial class AudioBand : TaskbarItemViewModel
    {
        MediaService _service = new();
        private DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();

        public AudioBand()
        {
            _service.InitializeAsync().ContinueWith(t => { UpdateTitle(); });


            _service.MediaSourcesChanged += MediaSourcesChanged;
            _service.CurrentMediaSourceChanged += CurrentMediaSourceChanged;
            _service.CurrentMediaPlaybackChanged += CurrentMediaPlaybackChanged;
        }

        private void CurrentMediaPlaybackChanged(object? sender, EventArgs e)
        {
            UpdateTitle();
        }

        private void CurrentMediaSourceChanged(object? sender, MediaSource? e)
        {
            UpdateTitle();
        }

        private void MediaSourcesChanged(object? sender, EventArgs e)
        {
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            _queue.TryEnqueue(UpdateTitleOnUiThread);
        }
        private void UpdateTitleOnUiThread()
        {
            if (_service.CurrentSource is MediaSource media)
            {
                Title = media.Name;
                Subtitle = media.Artist;
            }
            else
            {
                Title = "No media playing";
                Subtitle = string.Empty;
            }
        }
    }
}
