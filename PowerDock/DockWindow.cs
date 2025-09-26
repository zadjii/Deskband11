using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PowerDock
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DockWindow : Window
    {
        ObservableCollection<TaskbarApp> TaskbarItems { get; set; } = new ObservableCollection<TaskbarApp>();

        public DockWindow()
        {
            InitializeComponent();
        }

        [RelayCommand]
        void PressButton()
        {
            System.Collections.Generic.List<TaskbarApp> items = TaskbarApps.GetTaskbarWindows();
            TaskbarItems.Clear();
            foreach (TaskbarApp item in items)
            {
                TaskbarItems.Add(item);
            }
        }

        private void Button_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            PressButton();
        }
    }
}
