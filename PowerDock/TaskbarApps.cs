using Deskband.ViewModels;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace PowerDock
{
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
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}