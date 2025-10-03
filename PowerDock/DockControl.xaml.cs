using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace PowerDock;

public sealed partial class DockControl : UserControl, INotifyPropertyChanged
{
    private MainViewModel ViewModel;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Orientation ItemsOrientation
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(ItemsOrientation)));
            }
        }
    }

    internal DockControl(MainViewModel viewModel)
    {
        //MainViewModel mainModel = (MainViewModel)DataContext;
        ViewModel = viewModel;
        InitializeComponent();
    }

    internal void UpdateSettings(Settings settings)
    {
        bool isHorizontal = settings.Side == Side.Top || settings.Side == Side.Bottom;

        ItemsOrientation = isHorizontal ? Orientation.Horizontal : Orientation.Vertical;
    }
}