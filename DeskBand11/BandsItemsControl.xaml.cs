using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DeskBand11
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BandsItemsControl : UserControl
    {

        public ObservableCollection<TaskbarItemViewModel> Bands { get; set; }

        public BandsItemsControl()
        {
            Bands = new ObservableCollection<TaskbarItemViewModel>();
            Bands.Add(new ButtonsWithLabelsTaskBand());
            Bands.Add(new HelloWorldTaskBand());
            Bands.Add(new AudioBand());
            InitializeComponent();
        }
    }
}