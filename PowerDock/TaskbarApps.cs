using Deskband.ViewModels;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace PowerDock;


internal class MainViewModel : IDisposable
{
    private TaskbarWindowsService _taskbarWindows;
    private Settings _settings;
    private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public ObservableCollection<TaskbarItemViewModel> StartItems { get; } = new();
    public ObservableCollection<TaskbarItemViewModel> EndItems { get; } = new();

    public MainViewModel(Settings settings)
    {
        _settings = settings;
        _taskbarWindows = new(_settings);

        _taskbarWindows.Apps.CollectionChanged += Apps_CollectionChanged;

        EndItems.Add(new ButtonsWithLabelsTaskBand());

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
