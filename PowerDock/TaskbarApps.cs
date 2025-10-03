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

    public ObservableCollection<TaskbarItemViewModel> Items { get; } = new();
    public ObservableCollection<TaskbarApp> Apps => _taskbarWindows.Apps;

    public MainViewModel(Settings settings)
    {
        _settings = settings;
        _taskbarWindows = new(_settings);

        _taskbarWindows.Apps.CollectionChanged += Apps_CollectionChanged;
    }

    private void Apps_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Items.Clear();
        IEnumerable<TaskbarItemViewModel> appBands = _taskbarWindows.Apps.Select(AppToDeskband);
        foreach (TaskbarItemViewModel appBand in appBands)
        {
            Items.Add(appBand);
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