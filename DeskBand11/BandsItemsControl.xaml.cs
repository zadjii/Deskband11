using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;

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
        public IEnumerable<TaskbarItemViewModel> BandsDisplayOrder => Bands.Reverse();

        public BandsItemsControl()
        {
            Bands = new ObservableCollection<TaskbarItemViewModel>();

            // Add the TaskbarItemViewModel's you want here to extend the taskbar.

            // These two are testing samples
            Bands.Add(new ButtonsWithLabelsTaskBand());
            Bands.Add(new HelloWorldTaskBand());

            // This is the AudioBand recreation
            Bands.Add(new AudioBand());
            InitializeComponent();
        }

        private void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            Debug.WriteLine($"BandsItemsControl.ActualWidth={this.ActualWidth}");
        }

        public void SetMaxAvailableWidth(double availableSpace)
        {
            Debug.WriteLine($"SetMaxAvailableWidth({availableSpace})");

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

                    Debug.WriteLine($"  '{item.Title}' needs: {w}");
                    neededSpace += w;
                }
            }

            Debug.WriteLine($"  need: {neededSpace}");

            if (neededSpace <= availableSpace)
            {
                Debug.WriteLine($"  all fit");
                MoreButton.Visibility = Visibility.Collapsed;
                foreach (TaskbarItemViewModel item in BandsDisplayOrder)
                {
                    item.ShouldBeVisible = true;
                }
            }
            else
            {
                Debug.WriteLine($"  don't all fit");
                MoreButton.Visibility = Visibility.Visible;

                double takenSpace = MoreButton.Width;
                Debug.WriteLine($"    button: {takenSpace}");
                foreach (TaskbarItemViewModel item in BandsDisplayOrder)
                {
                    if (ItemsBar.ContainerFromItem(item) is FrameworkElement fwe)
                    {
                        Windows.Foundation.Size s = fwe.DesiredSize;
                        //double w = fwe.ActualWidth;
                        double w = s.Width;
                        Debug.WriteLine($"    {item.Title}: {w + takenSpace}");
                        if (takenSpace + w > availableSpace)
                        {
                            Debug.WriteLine($"      hide");

                            item.ShouldBeVisible = false;
                        }
                        takenSpace += w;
                    }
                }
            }
        }
    }
}