using JPSoftworks.MediaControlsExtension.Model;
using JPSoftworks.MediaControlsExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Dispatching;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DeskBand11
{
    public partial class HelloWorldTaskBand : TaskbarItemViewModel
    {
        public HelloWorldTaskBand()
        {
            Title = "Hello world";
        }
    }
    public partial class ButtonsWithLabelsTaskBand : TaskbarItemViewModel
    {
        public ButtonsWithLabelsTaskBand()
        {
            AnonymousCommand foo = new(() => { }) { Name = "Do nothing" };
            AnonymousCommand bar = new(() => { }) { Name = "Same", Icon = new("\uE98F") };
            Buttons.Add(new CommandViewModel(foo));
            Buttons.Add(new CommandViewModel(bar));
        }
    }

    public partial class AudioBand : TaskbarItemViewModel, IDisposable
    {
        MediaService _service = new();
        private DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();

        CommandViewModel _previousTrackButton;
        CommandViewModel _playButton;
        CommandViewModel _nextTrackButton;
        TogglePlayback _togglePlaybackCommand;

        MediaSource? _lastSource = null;

        public AudioBand()
        {
            _service.InitializeAsync().ContinueWith(t => { UpdateTitle(); });


            _service.MediaSourcesChanged += MediaSourcesChanged;
            _service.CurrentMediaSourceChanged += CurrentMediaSourceChanged;
            _service.CurrentMediaPlaybackChanged += CurrentMediaPlaybackChanged;

            _togglePlaybackCommand = new TogglePlayback(_service);
            _previousTrackButton = new(new PrevNextTrack(false, _service));
            _playButton = new(_togglePlaybackCommand);
            _nextTrackButton = new(new PrevNextTrack(true, _service));
        }

        private void CurrentMediaPlaybackChanged(object? sender, EventArgs e)
        {
            UpdateTitle();
        }

        private void CurrentMediaSourceChanged(object? sender, MediaSource? e)
        {
            UpdateTitle();
            if (_service.CurrentSource != _lastSource)
            {
                if (_lastSource != null)
                {
                    _lastSource.ThumbnailChanged -= ThumbnailChanged;
                }
                _lastSource = _service.CurrentSource;
                if (_lastSource != null)
                {
                    _lastSource.ThumbnailChanged += ThumbnailChanged;
                }
            }
        }

        private void MediaSourcesChanged(object? sender, EventArgs e)
        {
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            _queue.TryEnqueue(UpdateTitleOnUiThread);
        }

        private void ThumbnailChanged(object? sender, EventArgs e)
        {
            _queue.TryEnqueue(() => UpdateIconOnUiThread(_lastSource));
        }

        private void UpdateTitleOnUiThread()
        {
            if (_service.CurrentSource is MediaSource media)
            {
                Title = media.Name;
                Subtitle = media.Artist;

                CreateButtonsIfNeeded();
                _togglePlaybackCommand.SetIsPlaying(media.IsPlaying);

                // UpdateIconOnUiThread(media).ConfigureAwait(false);
            }
            else
            {
                Title = "No media playing";
                Subtitle = string.Empty;
                Icon = new(string.Empty);
                ClearButtons();
            }
        }
        private void UpdateIconOnUiThread(MediaSource? media)
        {
            Icon = media?.ThumbnailInfo?.Stream is IRandomAccessStream stream ? IconInfo.FromStream(stream) : new(string.Empty);
        }
        private void CreateButtonsIfNeeded()
        {
            if (Buttons.Count == 0)
            {
                Buttons.Add(_previousTrackButton);
                Buttons.Add(_playButton);
                Buttons.Add(_nextTrackButton);
            }

        }

        private void ClearButtons()
        {
            Buttons.Clear();
        }

        public void Dispose()
        {
            _service?.Dispose();
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
        private static readonly IconInfo PlayIcon = new("\uE768");
        private static readonly IconInfo PauseIcon = new("\uE769");

        // public override IconInfo Icon => PlayIcon;


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
            Icon = PlayIcon;
        }

        public void SetIsPlaying(bool isPlaying)
        {
            Icon = isPlaying ? PauseIcon : PlayIcon;
        }
    }
}