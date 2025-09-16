using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
        public partial string Title
        {
            get;
            set;
        }

        [ObservableProperty]
        public partial string Subtitle { get; set; }

        public virtual IProgressState? Progress { get; set; } = null;
        public virtual IconInfo HoverPreview { get; set; } = new IconInfo(string.Empty);
        public virtual ICommand? Command { get; set; } = null;
        public virtual ICommand[]? Buttons { get; set; } = null;

        IIconInfo ITaskbarItem.Icon => Icon;

        IIconInfo ITaskbarItem.HoverPreview => HoverPreview;

        ////
        // TODO! BODGY: CmdPal does this better, referencing actual theme
        public bool HasIcon => Icon != null && (!string.IsNullOrEmpty(Icon.Dark.Icon) || Icon.Dark.Data != null);
    }
}
