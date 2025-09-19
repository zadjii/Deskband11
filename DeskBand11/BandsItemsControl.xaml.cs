using CommunityToolkit.Mvvm.Messaging;
using Microsoft.CmdPal.Common.Services;
using Microsoft.CmdPal.UI.ViewModels.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DeskBand11
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BandsItemsControl : UserControl,
        IRecipient<OpenSettingsMessage>,
        IRecipient<SettingsChangedMessage>,
        INotifyPropertyChanged
    {
        public ObservableCollection<TaskbarItemViewModel> Bands { get; set; }

        public IEnumerable<TaskbarItemViewModel> BandsDisplayOrder => Bands.Where(b => b.IsEnabled).Reverse();

        private readonly ExtensionService _extensionService;
        private SettingsWindow? _settingsWindow = null;
        //private SettingsData? _settingsData = null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BandsItemsControl()
        {
            Bands = new ObservableCollection<TaskbarItemViewModel>();

            // Add the TaskbarItemViewModel's you want here to extend the taskbar.

            // These two are testing samples
            //Bands.Add(new ButtonsWithLabelsTaskBand());
            //Bands.Add(new HelloWorldTaskBand());

            // This is the AudioBand recreation
            Bands.Add(new AudioBand());

            _extensionService = new();
            _ = Task.Run(InitializeExtensions);
            //InitializeSettings();

            WeakReferenceMessenger.Default.Register<OpenSettingsMessage>(this);
            WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this);

            Bands.CollectionChanged += (s, e) => PropertyChanged?.Invoke(this, new(nameof(BandsDisplayOrder)));
            InitializeComponent();
        }

        private void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            Debug.WriteLine($"BandsItemsControl.ActualWidth={this.ActualWidth}");
        }

        private void Trace(string s)
        {
            if (false)
            {
                Debug.WriteLine(s);
            }
        }

        public void SetMaxAvailableWidth(double availableSpace)
        {
            Trace($"SetMaxAvailableWidth({availableSpace})");

            double neededSpace = 0.0;
            foreach (TaskbarItemViewModel item in BandsDisplayOrder)
            {
                if (ItemsBar.ContainerFromItem(item) is FrameworkElement fwe)
                {
                    item.ShouldBeVisible = true;
                    fwe.InvalidateMeasure();
                    fwe.Measure(new Windows.Foundation.Size(availableSpace, this.ActualHeight));
                    Windows.Foundation.Size s = fwe.DesiredSize;
                    //double w = fwe.ActualWidth;
                    double w = s.Width;

                    Trace($"  '{item.Title}' needs: {w}");
                    neededSpace += w;
                }
            }

            Trace($"  need: {neededSpace}");

            if (neededSpace <= availableSpace)
            {
                Trace($"  all fit");
                MoreButton.Visibility = Visibility.Collapsed;
                foreach (TaskbarItemViewModel item in BandsDisplayOrder)
                {
                    item.ShouldBeVisible = true;
                }
            }
            else
            {
                Trace($"  don't all fit");
                MoreButton.Visibility = Visibility.Visible;

                double takenSpace = MoreButton.Width;
                Trace($"    button: {takenSpace}");
                foreach (TaskbarItemViewModel item in BandsDisplayOrder)
                {
                    if (ItemsBar.ContainerFromItem(item) is FrameworkElement fwe)
                    {
                        Windows.Foundation.Size s = fwe.DesiredSize;
                        //double w = fwe.ActualWidth;
                        double w = s.Width;
                        Trace($"    {item.Title}: {w + takenSpace}");
                        if (takenSpace + w > availableSpace)
                        {
                            Trace($"      hide");

                            item.ShouldBeVisible = false;
                        }
                        takenSpace += w;
                    }
                }
            }
        }

        private async Task InitializeExtensions()
        {
            IEnumerable<IExtensionWrapper> extensions = await _extensionService.GetInstalledExtensionsAsync();
            foreach (IExtensionWrapper extension in extensions)
            {
                string json = extension.GetRegistrationJson();
                if (File.Exists(json))
                {
                    List<TaskbarItemViewModel> items = await JsonDeskband.JsonDeskbandLoader.LoadFromFileAsync(json, extension.PublicFolder);

                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        foreach (TaskbarItemViewModel item in items)
                        {
                            Bands.Add(item);
                            //_settingsData?.TaskbarItemStates?.Add(item.Id, new TaskbarItemState
                            //{
                            //    IsEnabled = item.IsEnabled,
                            //});
                        }
                    });
                }
                else
                {
                    Debug.WriteLine($"Could not find registration json at path: {json}");
                }
            }
        }

        public void Receive(OpenSettingsMessage message)
        {
            Debug.WriteLine("BandsItemsControl received OpenSettingsMessage");
            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow(this);
                _settingsWindow.Closed += (s, e) => _settingsWindow = null;
                _settingsWindow.Activate();
            }
            else
            {
                _settingsWindow.Activate();
            }
        }

        public void Receive(SettingsChangedMessage message)
        {
            PropertyChanged?.Invoke(this, new(nameof(BandsDisplayOrder)));
        }

        //private void InitializeSettings()
        //{
        //    new SettingsData
        //    {
        //        //AutoStart = AutoStartToggle.IsOn,
        //        //CheckForUpdates = UpdateCheckToggle.IsOn,
        //        //Theme = ThemeComboBox.SelectedIndex,
        //        TaskbarItemStates = Bands.ToDictionary(
        //            item => item.Id,
        //            item => new TaskbarItemState
        //            {
        //                IsEnabled = item.IsEnabled,
        //            })
        //    };
        //}
    }
    //#region Data Classes

    ///// <summary>
    ///// Data class to hold the current settings state.
    ///// </summary>
    //public class SettingsData
    //{
    //    //public bool AutoStart { get; set; }
    //    //public bool CheckForUpdates { get; set; }
    //    //public int Theme { get; set; }
    //    public Dictionary<string, TaskbarItemState> TaskbarItemStates { get; set; } = new();
    //}

    ///// <summary>
    ///// Data class to hold the state of a taskbar item.
    ///// </summary>
    //public class TaskbarItemState
    //{
    //    public bool IsEnabled { get; set; }
    //    //public bool ShouldBeVisible { get; set; }
    //}

    //#endregion

}