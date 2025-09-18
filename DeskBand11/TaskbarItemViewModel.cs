using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using Windows.Foundation;

namespace DeskBand11
{
    // If I had a CmdPal interface for this, the idl would look like this:
    /*
    enum ProgressDisplayState { OnIcon, Underneath };
    interface ITaskbarItem
    {
        String Id { get; }
        IIconInfo Icon { get; } // Height is set on this IconBox, but not width, so that you can have indicator pills
        String Title { get; }
        String Subtitle { get; }
        IProgressState Progress { get; }
        ProgressDisplayState ProgressDisplay { get; }
        IIconInfo HoverPreview { get; }
        ICommand Command { get; } // If this is set, the whole thing is a button that invokes that command
        ICommand[] Buttons { get; } // More buttons to show at the right of the button. Names are tooltips
    }
    */

    interface ITaskbarItem
    {
        string Id { get; }
        IIconInfo? Icon { get; } // Height is set on this IconBox, but not width, so that you can have indicator pills
        string Title { get; }
        string Subtitle { get; }
        IProgressState? Progress { get; }
        // Not sure what ProgressDisplayState ProgressDisplay was supposed to be
        IIconInfo? HoverPreview { get; }
        ICommand? Command { get; } // If this is set, the whole thing is a button that invokes that command
        ICommand[]? Buttons { get; } // More buttons to show at the right of the button. Names are tooltips
    }

    public partial class TaskbarItemViewModel : ObservableObject, ITaskbarItem
    {
        public virtual string Id { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasIcon))]
        public partial IconInfo Icon { get; set; } = new(string.Empty);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasText))]
        public partial string Title
        {
            get;
            set;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasText))]
        public partial string Subtitle { get; set; }

        public virtual IProgressState? Progress { get; set; } = null;
        public virtual IconInfo HoverPreview { get; set; } = new IconInfo(string.Empty);
        public virtual ICommand? Command { get; set; } = null;

        public ObservableCollection<CommandViewModel> Buttons = new();

        ICommand[]? ITaskbarItem.Buttons => Buttons.ToArray();

        IIconInfo ITaskbarItem.Icon => Icon;

        IIconInfo ITaskbarItem.HoverPreview => HoverPreview;

        //// Specifically view stuff 
        // TODO! BODGY: CmdPal does this better, referencing actual 
        public bool HasIcon => Icon != null && (!string.IsNullOrEmpty(Icon.Dark.Icon) || Icon.Dark.Data != null);
        public bool HasTitle => !string.IsNullOrEmpty(Title);
        public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
        public bool HasText => HasTitle || HasSubtitle;

        [ObservableProperty]
        public partial bool ShouldBeVisible { get; set; } = true;
    }

    public partial class CommandViewModel : ObservableObject, ICommand
    {
        private ICommand _model;
        private DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();

        public event TypedEventHandler<object, IPropChangedEventArgs>? PropChanged;

        public CommandViewModel(ICommand command)
        {
            _model = command;
            _model.PropChanged += Model_PropChanged;
        }

        private void Model_PropChanged(object sender, IPropChangedEventArgs args)
        {
            _queue.TryEnqueue(DispatcherQueuePriority.Normal, () => { OnPropertyChanged(args.PropertyName); });
        }

        public IIconInfo Icon => _model.Icon;

        public string Id => _model.Id;

        public string Name => _model.Name;

        // TODO! BODGY: CmdPal does this better, referencing actual theme
        public bool HasIcon => Icon != null && (!string.IsNullOrEmpty(Icon.Dark.Icon) || Icon.Dark.Data != null);

        [RelayCommand]
        public void Invoke()
        {
            _ = Task.Run(() =>
            {
                if (_model is IInvokableCommand invokable)
                {
                    invokable.Invoke(_model);
                }
            });
        }
    }
}
