using CommunityToolkit.Mvvm.Messaging;
using Deskband.ViewModels;
using Deskband.ViewModels.JsonDeskband;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using WinUIEx;

namespace DeskBand11
{
    /// <summary>
    /// Settings window for configuring DeskBand11 taskbar items and application settings.
    /// </summary>
    public sealed partial class SettingsWindow : WindowEx
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

            // // Initialize UI state
            // InitializeSettings();
        }

        private void SetupWindow()
        {
            this.ExtendsContentIntoTitleBar = true;
            this.Title = "DeskBand11 Settings";
            this.CenterOnScreen();
            this.SystemBackdrop = new MicaBackdrop();
        }

        // private void InitializeSettings()
        // {
        //     try
        //     {
        //         // Load saved settings if available
        //         _ = LoadUserSettings();
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.WriteLine($"Error initializing settings: {ex.Message}");
        //     }
        // }

        public static async Task<DeskbandSettings> LoadUserSettings()
        {
            try
            {
                string appData = Utilities.BaseSettingsPath("DeskBand11");
                string settingsPath = System.IO.Path.Combine(appData, "deskbandsettings.json");
                if (File.Exists(settingsPath))
                {
                    string json = await File.ReadAllTextAsync(settingsPath);
                    DeskbandSettings? settings = JsonSerializer.Deserialize<DeskbandSettings>(json, JsonDeskbandSourceGenerationContext.Default.DeskbandSettings);
                    if (settings is not null)
                    {
                        // // Apply loaded settings to taskbar items
                        // foreach (DeskbandItemSettings itemSettings in settings.Deskbands)
                        // {
                        //     TaskbarItems.FirstOrDefault(b => b.Id == itemSettings.Id)?.SetEnabled(itemSettings.Enabled);
                        // }
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user settings: {ex.Message}");
                //ShowErrorDialog("Failed to load settings", ex.Message);
            }

            return new DeskbandSettings();
        }

        private async Task SaveUserSettings()
        {
            try
            {
                DeskbandSettings settings = new();
                foreach (TaskbarItemViewModel band in TaskbarItems)
                {
                    if (!band.IsEnabled)
                    {
                        settings.Deskbands.Add(new DeskbandItemSettings() { Id = band.Id, IsEnabled = band.IsEnabled });
                    }
                }

                string json = JsonSerializer.Serialize(settings, JsonDeskbandSourceGenerationContext.Default.DeskbandSettings);
                string appData = Utilities.BaseSettingsPath("DeskBand11");
                string settingsPath = System.IO.Path.Combine(appData, "deskbandsettings.json");
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(settingsPath)!);
                await File.WriteAllTextAsync(settingsPath, json);

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