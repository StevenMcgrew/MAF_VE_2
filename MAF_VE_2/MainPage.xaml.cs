using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MAF_VE_2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            pivot_Main.SelectedItem = pivotItem_Search;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if ((page_Main.ActualWidth < 700) && (grid_ForMainPivotItem.Children.Contains(sPanel_Search)))
            {
                grid_ForMainPivotItem.Children.Remove(sPanel_Search);
                grid_ForSearchPivotItem.Children.Add(sPanel_Search);

                grid_ForMainPivotItem.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Pixel);
                grid_ForMainPivotItem.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            }
            else if ((page_Main.ActualWidth >= 687) && !(grid_ForMainPivotItem.Children.Contains(sPanel_Search)))
            {
                grid_ForSearchPivotItem.Children.Remove(sPanel_Search);
                grid_ForMainPivotItem.Children.Add(sPanel_Search);

                grid_ForMainPivotItem.RowDefinitions[0].Height = GridLength.Auto;
                grid_ForMainPivotItem.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            }
        }

        private void btnCloseRecordsView2_Click(object sender, RoutedEventArgs e)
        {
            pivot_Main.SelectedItem = pivotItem_Calculator;
        }
    }
}
