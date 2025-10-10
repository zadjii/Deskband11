using CommunityToolkit.Mvvm.Messaging;
using Deskband.ViewModels;
using DeskBand.ViewModels.Messages;
using DeskBand11;
using Microsoft.CmdPal.Ext.WindowWalker;
using Microsoft.CmdPal.Ext.WindowWalker.Helpers;
using Microsoft.CmdPal.Ext.WindowWalker.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.ObjectModel;
using Windows.Storage.Streams;
using WindowsDesktop;

namespace PowerDock;


internal class MainViewModel : IDisposable
{
    private TaskbarWindowsService _taskbarWindows;
    private Settings _settings;
    private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    public ObservableCollection<TaskbarItemViewModel> StartItems { get; } = new();
    public ObservableCollection<TaskbarItemViewModel> EndItems { get; } = new();

    private WindowWalkerListPage _ww;
    private VirtualDesktopsListPage _vd;

    public MainViewModel(Settings settings)
    {
        _settings = settings;
        _taskbarWindows = new(_settings);

        _taskbarWindows.Apps.CollectionChanged += Apps_CollectionChanged;

        //EndItems.Add(new AudioBand());
        //EndItems.Add(new ClockTaskBand());
        //EndItems.Add(new SettingsTaskBand());

        SettingsManager.Instance.InMruOrder = false;
        SettingsManager.Instance.ResultsFromVisibleDesktopOnly = true;
        _ww = new WindowWalkerListPage();
        _vd = new();
        _ww.ItemsChanged += WindowsChanged;
        _vd.ItemsChanged += DesktopsChanged; ;
        RegenWindows();
        RegenEndItems();
    }

    private void DesktopsChanged(object sender, IItemsChangedEventArgs args)
    {
        dispatcherQueue.TryEnqueue(() => RegenEndItems());

    }

    private void RegenEndItems()
    {
        EndItems.Clear();
        IListItem[] desktopItems = _vd.GetItems();
        foreach (IListItem vd in desktopItems)
        {
            EndItems.Add(ListItemToDeskband(vd));
        }

        EndItems.Add(new AudioBand());
        EndItems.Add(new ClockTaskBand());
        EndItems.Add(new SettingsTaskBand());

    }

    private void WindowsChanged(object sender, Microsoft.CommandPalette.Extensions.IItemsChangedEventArgs args)
    {
    }

    private void RegenWindows()
    {
        StartItems.Clear();
        Microsoft.CommandPalette.Extensions.IListItem[] items = _ww.GetItems();
        foreach (Microsoft.CommandPalette.Extensions.IListItem item in items)
        {
            string title = _settings.ShowAppTitles ? item.Title : string.Empty;
            WindowWalkerListItem? li = item as WindowWalkerListItem;
            IconInfo icon = new(".");
            if (li != null)
            {
                nint hwnd = li.Window?.Hwnd ?? IntPtr.Zero;
                nint hIcon = TaskbarWindowsService.GetWindowIcon(hwnd);
                IRandomAccessStream? iconStream = hIcon != IntPtr.Zero ? TaskbarWindowsService.ConvertIconToStream(hIcon) : null;
                if (iconStream != null)
                {
                    icon = IconInfo.FromStream(iconStream);
                }
            }

            TaskbarItemViewModel tvi = new()
            {
                Title = title,
                Subtitle = string.Empty,
                Icon = icon, // new("."),// ((Command)item.Command).Icon,
                Command = item.Command
            };
            //var commandIcon = (item as)
            //Command? switchToCommand = item.Command as Command;
            //if (switchToCommand != null)
            //{
            //    tvi.Icon = switchToCommand.Icon;
            //}
            StartItems.Add(tvi);
        }
    }

    private void Apps_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // RegenerateApps();
        RegenWindows();
    }
    public void UpdateSettings()
    {
        // RegenerateApps();
        RegenWindows();
    }
    private void RegenerateApps()
    {
        StartItems.Clear();
        IEnumerable<TaskbarItemViewModel> appBands = _taskbarWindows.Apps.Select(AppToDeskband);
        foreach (TaskbarItemViewModel appBand in appBands)
        {
            StartItems.Add(appBand);
        }
    }


    private TaskbarItemViewModel AppToDeskband(TaskbarApp app)
    {
        return new TaskbarItemViewModel()
        {
            Title = _settings.ShowAppTitles ? app.Title : string.Empty,
            Subtitle = string.Empty,
            Icon = app.Icon,
            Command = new AnonymousCommand(() => app.SwitchToCommand.Execute(null))
        };

    }

    private TaskbarItemViewModel ListItemToDeskband(IListItem item)
    {
        return new TaskbarItemViewModel()
        {
            Title = item.Title,
            Subtitle = item.Subtitle,
            Icon = new(item.Icon.Dark.Icon), // TODO! hack
            Command = item.Command,
        };

    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}


public partial class ButtonsWithLabelsTaskBand : TaskbarItemViewModel
{
    public override string Id => "builtin.ButtonsWithLabelsTaskBand";
    public ButtonsWithLabelsTaskBand()
    {
        AnonymousCommand foo = new(() => { }) { Name = "Do nothing" };
        AnonymousCommand bar = new(() => { }) { Name = "Same", Icon = new("\uE98F") };
        Buttons.Add(new CommandViewModel(foo));
        Buttons.Add(new CommandViewModel(bar));
    }
}

public partial class SettingsTaskBand : TaskbarItemViewModel
{
    public override string Id => "builtin.SettingsTaskBand";
    public SettingsTaskBand()
    {
        Command = new AnonymousCommand(() => WeakReferenceMessenger.Default.Send<OpenSettingsMessage>(new()));
        Icon = new IconInfo("\uE713");
    }
}

public partial class ClockTaskBand : TaskbarItemViewModel
{
    private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    public override string Id => "builtin.ClockTaskBand";
    public ClockTaskBand()
    {
        Title = DateTime.Now.ToString("HH:mm");
        Subtitle = DateTime.Now.ToString("ddd dd MMM");

        // Create a timer to update the time every minute
        System.Timers.Timer timer = new(60000); // 60000 ms = 1 minute
        // but we want it to tick on the minute, so calculate the initial delay
        DateTime now = DateTime.Now;
        timer.Interval = 60000 - (now.Second * 1000 + now.Millisecond);
        // then after the first tick, set it to 60 seconds

        timer.Elapsed += Timer_ElapsedFirst;
        timer.Start();
    }

    private void Timer_ElapsedFirst(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // After the first tick, set the interval to 60 seconds
        System.Timers.Timer timer = (System.Timers.Timer)sender;
        timer.Interval = 60000;
        timer.Elapsed -= Timer_ElapsedFirst;
        timer.Elapsed += Timer_Elapsed;
        Timer_Elapsed(sender, e);
    }
    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        dispatcherQueue.TryEnqueue(() =>
        {
            Title = DateTime.Now.ToString("HH:mm");
            Subtitle = DateTime.Now.ToString("ddd dd MMM");

        });
    }
}

public partial class VirtualDesktopsListPage : ListPage
{
    public static readonly IconInfo CheckboxEmptyIcon = new("\uE739");
    public static readonly IconInfo CheckboxFillIcon = new("\uE73B");
    public static readonly IconInfo ToggleFilledIcon = new("\uEC11");
    public static readonly IconInfo StatusCircleIcon = new("\uEA81");
    public static readonly IconInfo CircleFillBadge12Icon = new("\uEDB0");

    public VirtualDesktopsListPage()
    {
        VirtualDesktop.CurrentChanged += (_, args) => RaiseItemsChanged();
        VirtualDesktop.Created += (_, desktop) => RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        VirtualDesktop[] desktops = VirtualDesktop.GetDesktops();
        List<IListItem> items = new(desktops.Length);
        foreach (VirtualDesktop desktop in desktops)
        {
            items.Add(DesktopToItem(desktop));
        }
        return items.ToArray();
    }

    private IListItem DesktopToItem(VirtualDesktop desktop)
    {
        bool isCurrent = desktop == VirtualDesktop.Current;
        return new ListItem(new AnonymousCommand(() => desktop.Switch()) { Name = string.Empty })
        {
            // Icon = isCurrent ? CheckboxFillIcon : CheckboxEmptyIcon
            Icon = isCurrent ? ToggleFilledIcon : CircleFillBadge12Icon
        };
    }
}