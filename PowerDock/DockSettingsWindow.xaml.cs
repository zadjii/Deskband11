using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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
        };

        DockSizeComboBox.SelectionChanged += (s, e) =>
        {
            Settings.DockSize = SelectedIndexToDockSize(DockSizeComboBox.SelectedIndex);
            UpdatePreviewText();
            _parentWindow.RefreshSettings();
        };

        DockPositionComboBox.SelectionChanged += (s, e) =>
        {
            Settings.Side = SelectedIndexToSide(DockPositionComboBox.SelectedIndex);
            UpdatePreviewText();
            _parentWindow.RefreshSettings();
        };

        BackdropComboBox.SelectionChanged += (s, e) =>
        {
            Settings.Backdrop = SelectedIndexToBackdrop(BackdropComboBox.SelectedIndex);
            UpdatePreviewText();
            _parentWindow.RefreshSettings();
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

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset to default values
        Settings.ShowAppTitles = false;
        Settings.Side = Side.Top;
        Settings.DockSize = DockSize.Small;
        Settings.Backdrop = DockBackdrop.Acrylic;

        // Update UI to reflect the reset values
        ShowAppTitlesToggle.IsOn = Settings.ShowAppTitles;
        DockSizeComboBox.SelectedIndex = SelectedDockSizeIndex;
        DockPositionComboBox.SelectedIndex = SelectedSideIndex;
        BackdropComboBox.SelectedIndex = SelectedBackdropIndex;

        UpdatePreviewText();
        _parentWindow.RefreshSettings();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}