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

    private CmdPalStartListPage _cmdPal;
    private WindowWalkerListPage _ww;
    private VirtualDesktopsListPage _vd;

    private TaskbarItemViewModel _cmdPalBand;
    private TaskbarItemViewModel _settingsBand;
    private TaskbarItemViewModel _clockBand;
    private TaskbarItemViewModel _audioBand;


    public MainViewModel(Settings settings)
    {
        _settings = settings;
        _taskbarWindows = new(_settings);

        _taskbarWindows.Apps.CollectionChanged += Apps_CollectionChanged;

        _cmdPal = new();
        _cmdPalBand = ListItemToDeskband(_cmdPal.GetItems()[0]);
        _settingsBand = new SettingsTaskBand();
        _clockBand = new ClockTaskBand();
        _audioBand = new AudioBand();

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

        List<TaskbarItemViewModel> newItems = new();

        IListItem[] desktopItems = _vd.GetItems();
        foreach (IListItem vd in desktopItems)
        {
            newItems.Add(ListItemToDeskband(vd));
        }

        newItems.Add(_audioBand);
        newItems.Add(_clockBand);
        newItems.Add(_settingsBand);

        ListHelpers.InPlaceUpdateList(EndItems, newItems);
    }

    private void WindowsChanged(object sender, Microsoft.CommandPalette.Extensions.IItemsChangedEventArgs args)
    {
    }

    private void RegenWindows()
    {
        StartItems.Clear();
        StartItems.Add(_cmdPalBand);

        Microsoft.CommandPalette.Extensions.IListItem[] items = _ww.GetItems();
        foreach (Microsoft.CommandPalette.Extensions.IListItem item in items)
        {
            string title = _settings.ShowAppTitles ? item.Title : string.Empty;
            WindowWalkerListItem li = (item as WindowWalkerListItem)!;
            TaskbarItemViewModel tvi = ListItemToDeskband(li);

            IconInfo icon = new(".");
            nint hwnd = li.Window?.Hwnd ?? IntPtr.Zero;
            nint hIcon = TaskbarWindowsService.GetWindowIcon(hwnd);
            IRandomAccessStream? iconStream = hIcon != IntPtr.Zero ? TaskbarWindowsService.ConvertIconToStream(hIcon) : null;
            if (iconStream != null)
            {
                icon = IconInfo.FromStream(iconStream);
            }
            tvi.Icon = icon;
            tvi.Subtitle = string.Empty;

            //TaskbarItemViewModel tvi = new()
            //{
            //    Title = title,
            //    Subtitle = string.Empty,
            //    Icon = icon, // new("."),// ((Command)item.Command).Icon,
            //    Command = item.Command
            //};
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

    public static TaskbarItemViewModel ListItemToDeskband(IListItem item)
    {
        IEnumerable<TaskbarItemViewModel> contextMenu = item.MoreCommands.Select(i => ContextItemToDeskband(i)).Where(i => i != null).Select(i => i!);
        IconInfo icon = new(string.Empty);
        if (item.Icon is IconInfo ii)
        {
            icon = ii;
        }
        return new TaskbarItemViewModel()
        {
            Title = item.Title,
            Subtitle = item.Subtitle,
            Icon = icon, // TODO! hack
            Command = item.Command,
            ContextMenu = new(contextMenu)
        };

    }
    public static TaskbarItemViewModel? ContextItemToDeskband(IContextItem context)
    {
        if (context is ICommandContextItem item)
        {
            IEnumerable<TaskbarItemViewModel> contextMenu = item.MoreCommands.Select(i => ContextItemToDeskband(i)).Where(i => i != null).Select(i => i!);
            IconInfo icon = new(string.Empty);
            if (item.Icon is IconInfo ii)
            {
                icon = ii;
            }
            else if (item.Command.Icon is IconInfo ii2)
            {
                icon = ii2;
            }
            return new TaskbarItemViewModel()
            {
                Title = item.Title,
                Subtitle = item.Subtitle,
                Icon = icon, // TODO! hack
                Command = item.Command,
                ContextMenu = new(contextMenu)
            };

        }

        return null;

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

        AnonymousCommand postQuitCommand = new(() => { WeakReferenceMessenger.Default.Send<QuitMessage>(); }) { Name = "Quit" };
        ListItem quitLi = new(postQuitCommand);
        TaskbarItemViewModel quitTvi = MainViewModel.ListItemToDeskband(quitLi);

        this.ContextMenu = new([quitTvi]);
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

public partial class CmdPalStartListPage : ListPage
{
    private InvokableCommand _openCmdPal;
    private ListItem _listItem;

    public CmdPalStartListPage()
    {

        _openCmdPal = new AnonymousCommand(() =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "x-cmdpal://",
                UseShellExecute = true
            });
        })
        { Name = string.Empty };

        _listItem = new ListItem(_openCmdPal) { Icon = new IconInfo("https://raw.githubusercontent.com/microsoft/PowerToys/refs/heads/main/src/modules/cmdpal/Microsoft.CmdPal.UI/Assets/Stable/icon.svg") };
    }

    public override IListItem[] GetItems()
    {
        return new[] { _listItem };
    }
}