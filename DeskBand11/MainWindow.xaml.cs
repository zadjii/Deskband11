using JPSoftworks.MediaControlsExtension.Model;
using JPSoftworks.MediaControlsExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage.Streams;
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
            this.Root.SizeChanged += ItemsBar_SizeChanged;
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

            Debug.WriteLine($"ActualWidth: {clipToElement.ActualWidth}");
            Debug.WriteLine($"scaledBounds.Width: {scaledBounds.Width} ({scaledBounds.Width / scaleFactor})");

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

                Icon = media.ThumbnailInfo?.Stream is IRandomAccessStream stream ? IconInfo.FromStream(stream) : new(string.Empty);
                CreateButtonsIfNeeded();
            }
            else
            {
                Title = "No media playing";
                Subtitle = string.Empty;
                Icon = new(string.Empty);
                ClearButtons();
            }
        }

        private void CreateButtonsIfNeeded()
        {
            if (Buttons.Count == 0)
            {
                PrevNextTrack prev = new(false, _service);
                TogglePlayback play = new(_service);
                PrevNextTrack next = new(true, _service);
                CommandViewModel previousTrackButton = new(prev);
                CommandViewModel playButton = new(play);
                CommandViewModel nextTrackButton = new(next);
                Buttons.Add(previousTrackButton);
                Buttons.Add(playButton);
                Buttons.Add(nextTrackButton);
            }

        }

        private void ClearButtons()
        {
            Buttons.Clear();
        }
    }

    internal partial class PrevNextTrack : InvokableCommand
    {
        private readonly bool _next = false;
        private readonly MediaService _service;

        public override IconInfo Icon => _next ? new IconInfo("\uE893") : new IconInfo("\uE892");


        public override ICommandResult Invoke()
        {
            Windows.Media.Control.GlobalSystemMediaTransportControlsSession? session = _service.CurrentSource?.Session;
            if (session == null)
            {
                return CommandResult.KeepOpen();
            }

            if (_next)
            {
                session.TrySkipNextAsync().AsTask().ConfigureAwait(false);
            }
            else
            {
                session.TrySkipPreviousAsync().AsTask().ConfigureAwait(false);
            }
            return CommandResult.KeepOpen();
        }

        internal PrevNextTrack(bool next, MediaService service)
        {
            _next = next;
            _service = service;
        }
    }
    internal partial class TogglePlayback : InvokableCommand
    {
        private readonly MediaService _service;

        public override IconInfo Icon => new("\uE768");


        public override ICommandResult Invoke()
        {
            Windows.Media.Control.GlobalSystemMediaTransportControlsSession? session = _service.CurrentSource?.Session;
            if (session == null)
            {
                return CommandResult.KeepOpen();
            }

            session.TryTogglePlayPauseAsync().AsTask().ConfigureAwait(false);
            return CommandResult.KeepOpen();
        }

        internal TogglePlayback(MediaService service)
        {
            _service = service;
        }
    }

}
