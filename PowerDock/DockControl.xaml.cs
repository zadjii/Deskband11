using CommunityToolkit.Mvvm.Input;
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

    public bool ShowSearchButton
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(ShowSearchButton)));
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
        ViewModel.UpdateSettings();
        ItemsOrientation = isHorizontal ? Orientation.Horizontal : Orientation.Vertical;
        ShowSearchButton = settings.ShowSearchButton;
        SearchColumn.Width = ShowSearchButton
            ? new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star)
            : new Microsoft.UI.Xaml.GridLength(0, Microsoft.UI.Xaml.GridUnitType.Star);

        EndColumn.Width = ShowSearchButton
            ? new Microsoft.UI.Xaml.GridLength(2, Microsoft.UI.Xaml.GridUnitType.Star)
            : new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Auto);
    }

    [RelayCommand]
    private void SearchOpenCmdPal(object? whatever)
    {
        // URI invoke "x-cmdpal://"
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "x-cmdpal://",
            UseShellExecute = true
        });
    }
}