using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Text.Json;
using WinUIEx;

namespace PowerDock;

/// <summary>
/// Settings window for configuring PowerDock appearance and behavior.
/// </summary>
internal sealed partial class DockSettingsWindow : WindowEx
{
    public Settings Settings { get; }

    private readonly DockWindow _parentWindow;

    public DockSettingsWindow(DockWindow parentWindow, Settings settings)
    {
        _parentWindow = parentWindow;
        Settings = settings;

        this.InitializeComponent();

        // Set up window properties
        SetupWindow();

        // Initialize UI state
        InitializeSettings();

        // Set up event handlers for real-time preview
        SetupPreviewHandlers();
    }

    private void SetupWindow()
    {
        this.ExtendsContentIntoTitleBar = true;
        this.Title = "PowerDock Settings";
        this.CenterOnScreen();
        this.SystemBackdrop = new MicaBackdrop();

        // Make sure the window stays on top but not always on top
        this.IsAlwaysOnTop = false;
    }

    private void InitializeSettings()
    {
        // Initialize UI controls to match current settings
        ShowAppTitlesToggle.IsOn = Settings.ShowAppTitles;
        ShowSearchButtonToggle.IsOn = Settings.ShowSearchButton;
        DockSizeComboBox.SelectedIndex = SelectedDockSizeIndex;
        DockPositionComboBox.SelectedIndex = SelectedSideIndex;
        BackdropComboBox.SelectedIndex = SelectedBackdropIndex;

        UpdatePreviewText();
    }

    private void SetupPreviewHandlers()
    {
        // Set up event handlers to update settings in real-time
        ShowAppTitlesToggle.Toggled += (s, e) =>
        {
            Settings.ShowAppTitles = ShowAppTitlesToggle.IsOn;
            UpdatePreviewText();
            _parentWindow.RefreshSettings();
            SaveUserSettingsAsync(Settings).ConfigureAwait(false);
        };

        ShowSearchButtonToggle.Toggled += (s, e) =>
        {
            Settings.ShowSearchButton = ShowSearchButtonToggle.IsOn;
            UpdatePreviewText();
            _parentWindow.RefreshSettings();
            SaveUserSettingsAsync(Settings).ConfigureAwait(false);
        };

        DockSizeComboBox.SelectionChanged += (s, e) =>
        {
            Settings.DockSize = SelectedIndexToDockSize(DockSizeComboBox.SelectedIndex);
            UpdatePreviewText();
            _parentWindow.RefreshSettings();
            SaveUserSettingsAsync(Settings).ConfigureAwait(false);
        };

        DockPositionComboBox.SelectionChanged += (s, e) =>
        {
            Settings.Side = SelectedIndexToSide(DockPositionComboBox.SelectedIndex);
            UpdatePreviewText();
            _parentWindow.RefreshSettings();
            SaveUserSettingsAsync(Settings).ConfigureAwait(false);
        };

        BackdropComboBox.SelectionChanged += (s, e) =>
        {
            Settings.Backdrop = SelectedIndexToBackdrop(BackdropComboBox.SelectedIndex);
            UpdatePreviewText();
            _parentWindow.RefreshSettings();
            SaveUserSettingsAsync(Settings).ConfigureAwait(false);
        };
    }

    private void UpdatePreviewText()
    {
        PreviewSizeText.Text = Settings.DockSize.ToString();
        PreviewPositionText.Text = Settings.Side.ToString();
    }

    // Property bindings for ComboBoxes
    public int SelectedDockSizeIndex
    {
        get => DockSizeToSelectedIndex(Settings.DockSize);
        set => Settings.DockSize = SelectedIndexToDockSize(value);
    }

    public int SelectedSideIndex
    {
        get => SideToSelectedIndex(Settings.Side);
        set => Settings.Side = SelectedIndexToSide(value);
    }

    public int SelectedBackdropIndex
    {
        get => BackdropToSelectedIndex(Settings.Backdrop);
        set => Settings.Backdrop = SelectedIndexToBackdrop(value);
    }

    // Conversion methods for ComboBox bindings
    private static int DockSizeToSelectedIndex(DockSize size) => size switch
    {
        DockSize.Small => 0,
        DockSize.Medium => 1,
        DockSize.Large => 2,
        _ => 0
    };

    private static DockSize SelectedIndexToDockSize(int index) => index switch
    {
        0 => DockSize.Small,
        1 => DockSize.Medium,
        2 => DockSize.Large,
        _ => DockSize.Small
    };

    private static int SideToSelectedIndex(Side side) => side switch
    {
        Side.Left => 0,
        Side.Top => 1,
        Side.Right => 2,
        Side.Bottom => 3,
        _ => 1
    };

    private static Side SelectedIndexToSide(int index) => index switch
    {
        0 => Side.Left,
        1 => Side.Top,
        2 => Side.Right,
        3 => Side.Bottom,
        _ => Side.Top
    };

    private static int BackdropToSelectedIndex(DockBackdrop backdrop) => backdrop switch
    {
        DockBackdrop.Mica => 0,
        DockBackdrop.Transparent => 1,
        DockBackdrop.Acrylic => 2,
        _ => 2
    };

    private static DockBackdrop SelectedIndexToBackdrop(int index) => index switch
    {
        0 => DockBackdrop.Mica,
        1 => DockBackdrop.Transparent,
        2 => DockBackdrop.Acrylic,
        _ => DockBackdrop.Acrylic
    };

    private static string GetSettingsPath()
    {
        string appData = Utilities.BaseSettingsPath("PowerDock");
        string settingsPath = System.IO.Path.Combine(appData, "dock_settings.json");
        return settingsPath;
    }

    /// <summary>
    /// Load PowerDock settings from JSON file
    /// </summary>
    /// <returns>The loaded settings or default settings if loading fails</returns>
    public static Settings LoadUserSettings()
    {
        try
        {
            string settingsPath = GetSettingsPath();

            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
                Settings? settings = JsonSerializer.Deserialize<Settings>(json, PowerDockSourceGenerationContext.Default.Settings);

                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading PowerDock settings: {ex.Message}");
        }

        return new Settings(); // Return default settings if loading fails
    }

    /// <summary>
    /// Save PowerDock settings to JSON file
    /// </summary>
    /// <param name="settings">The settings to save</param>
    public static async Task SaveUserSettingsAsync(Settings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, PowerDockSourceGenerationContext.Default.Settings);
            string settingsPath = GetSettingsPath();

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

            await File.WriteAllTextAsync(settingsPath, json);
            Debug.WriteLine("PowerDock settings saved successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving PowerDock settings: {ex.Message}");
            throw;
        }
    }

    // private async void SaveButton_Click(object sender, RoutedEventArgs e)
    // {
    //     try
    //     {
    //         await SaveUserSettingsAsync(Settings);
    //         _parentWindow.RefreshSettings();
    //         ShowSuccessDialog("Settings saved successfully!");
    //     }
    //     catch (Exception ex)
    //     {
    //         Debug.WriteLine($"Error saving settings: {ex.Message}");
    //         ShowErrorDialog("Failed to save settings", ex.Message);
    //     }
    // }

    // private async void ShowErrorDialog(string title, string message)
    // {
    //     ContentDialog dialog = new()
    //     {
    //         Title = title,
    //         Content = message,
    //         CloseButtonText = "OK",
    //         XamlRoot = this.Content.XamlRoot
    //     };

    //     await dialog.ShowAsync();
    // }

    // private async void ShowSuccessDialog(string message)
    // {
    //     ContentDialog dialog = new()
    //     {
    //         Title = "Success",
    //         Content = message,
    //         CloseButtonText = "OK",
    //         XamlRoot = this.Content.XamlRoot
    //     };

    //     await dialog.ShowAsync();
    // }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset to default values
        Settings.ShowAppTitles = false;
        Settings.ShowSearchButton = true;
        Settings.Side = Side.Top;
        Settings.DockSize = DockSize.Small;
        Settings.Backdrop = DockBackdrop.Acrylic;

        // Update UI to reflect the reset values
        ShowAppTitlesToggle.IsOn = Settings.ShowAppTitles;
        ShowSearchButtonToggle.IsOn = Settings.ShowSearchButton;
        DockSizeComboBox.SelectedIndex = SelectedDockSizeIndex;
        DockPositionComboBox.SelectedIndex = SelectedSideIndex;
        BackdropComboBox.SelectedIndex = SelectedBackdropIndex;

        UpdatePreviewText();
        _parentWindow.RefreshSettings();

        // Save the reset settings
        try
        {
            await SaveUserSettingsAsync(Settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving reset settings: {ex.Message}");
        }
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Save settings before closing
        try
        {
            await SaveUserSettingsAsync(Settings);
            _parentWindow.RefreshSettings();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving settings on close: {ex.Message}");
        }

        this.Close();
    }
}