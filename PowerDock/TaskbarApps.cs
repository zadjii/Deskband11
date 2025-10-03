using CommunityToolkit.Mvvm.Messaging;
using Deskband.ViewModels;
using DeskBand.ViewModels.Messages;
using DeskBand11;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.ObjectModel;

namespace PowerDock;


internal class MainViewModel : IDisposable
{
    private TaskbarWindowsService _taskbarWindows;
    private Settings _settings;
    private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    public ObservableCollection<TaskbarItemViewModel> StartItems { get; } = new();
    public ObservableCollection<TaskbarItemViewModel> EndItems { get; } = new();

    public MainViewModel(Settings settings)
    {
        _settings = settings;
        _taskbarWindows = new(_settings);

        _taskbarWindows.Apps.CollectionChanged += Apps_CollectionChanged;

        EndItems.Add(new AudioBand());
        EndItems.Add(new ClockTaskBand());
        EndItems.Add(new SettingsTaskBand());

    }

    private void Apps_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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