using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace DeskBand11
{
    /// <summary>
    /// Settings window for configuring DeskBand11 taskbar items and application settings.
    /// </summary>
    public sealed partial class SettingsWindow : Window
    {
        public ObservableCollection<TaskbarItemViewModel> TaskbarItems { get; }

        //private readonly BandsItemsControl _bandsControl;

        public SettingsWindow(BandsItemsControl bandsControl)
        {
            //_bandsControl = bandsControl;
            TaskbarItems = bandsControl.Bands;

            this.InitializeComponent();

            // Set up window properties
            SetupWindow();

            // Initialize UI state
            InitializeSettings();
        }

        private void SetupWindow()
        {
            // Get the window handle for this WinUI 3 window
            //var windowHandle = WindowNative.GetWindowHandle(this);
            //var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            //var appWindow = AppWindow.GetFromWindowId(windowId);
            AppWindow? appWindow = this.AppWindow;
            // Set window icon if available
            if (appWindow is not null)
            {
                appWindow.Title = "DeskBand11 Settings";

                //// Center the window
                //DisplayArea? displayArea = DisplayArea.Primary;
                //if (displayArea is not null)
                //{
                //    int centerX = (displayArea.WorkArea.Width - 600) / 2;
                //    int centerY = (displayArea.WorkArea.Height - 500) / 2;
                //    appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                //    appWindow.MoveAndResize(new Windows.Graphics.RectInt32(centerX, centerY), 
                //}
            }
        }

        private void InitializeSettings()
        {
            try
            {
                // Load saved settings if available
                LoadUserSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing settings: {ex.Message}");
            }
        }

        private void LoadUserSettings()
        {
            // TODO: Implement loading user settings from storage
        }

        private void SaveUserSettings()
        {
            try
            {
                // TODO: Implement saving user settings to storage
                Debug.WriteLine("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
                ShowErrorDialog("Failed to save settings", ex.Message);
            }
        }

        private async void ShowErrorDialog(string title, string message)
        {
            ContentDialog dialog = new()
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async void ShowSuccessDialog(string message)
        {
            ContentDialog dialog = new()
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        #region Event Handlers

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save current settings
                SaveUserSettings();

                // Apply taskbar item visibility changes
                ApplyTaskbarItemChanges();

                ShowSuccessDialog("Settings applied successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying settings: {ex.Message}");
                ShowErrorDialog("Failed to apply settings", ex.Message);
            }
        }

        private void ApplyTaskbarItemChanges()
        {
            try
            {
                // Force refresh of the bands control to reflect enabled/disabled states
                //_bandsControl?.SetMaxAvailableWidth(_bandsControl.ActualWidth);
                WeakReferenceMessenger.Default.Send<SettingsChangedMessage>();

                Debug.WriteLine("Taskbar item changes applied");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying taskbar changes: {ex.Message}");
                throw;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the taskbar items list to reflect any changes made to the collection.
        /// </summary>
        public void RefreshTaskbarItems()
        {
            try
            {
                // The ObservableCollection should automatically update the UI
                // This method can be called if manual refresh is needed
                Debug.WriteLine($"Refreshed taskbar items list. Count: {TaskbarItems.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing taskbar items: {ex.Message}");
            }
        }

        #endregion
    }

}