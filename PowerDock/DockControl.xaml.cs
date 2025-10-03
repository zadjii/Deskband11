using Microsoft.UI.Xaml.Controls;

namespace PowerDock;

public sealed partial class DockControl : UserControl
{
    private MainViewModel ViewModel;

    internal DockControl(MainViewModel viewModel)
    {
        //MainViewModel mainModel = (MainViewModel)DataContext;
        ViewModel = viewModel;
        InitializeComponent();
    }

    internal void UpdateSettings(Settings settings)
    {
        bool isHorizontal = settings.Side == Side.Top || settings.Side == Side.Bottom;

        if (PanelLayout is not null)
        {
            PanelLayout.Orientation = isHorizontal ? Orientation.Horizontal : Orientation.Vertical;
        }

    }
}