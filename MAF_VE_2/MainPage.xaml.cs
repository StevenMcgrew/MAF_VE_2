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

namespace MAF_VE_2
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void mainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //mainPage.SizeChanged += mainPage_SizeChanged;
        }

        private void mainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (mainPage.ActualWidth < 640)
            {
                if (mainGrid.Children.Contains(recordsPanel))
                {
                    mainGrid.Children.Remove(recordsPanel);
                    recordsGrid.Children.Add(recordsPanel);
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                }
            }
            else
            {
                if (recordsGrid.Children.Contains(recordsPanel))
                {
                    recordsGrid.Children.Remove(recordsPanel);
                    mainGrid.Children.Add(recordsPanel);
                    mainGrid.ColumnDefinitions[0].Width = GridLength.Auto;
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                }
            }

            if ((mainPage.ActualHeight < 700) && (mainPage.ActualWidth < 640))
            {
                if (leftSlimPanel.Children.Contains(searchPanel))
                {
                    leftSlimPanel.Children.Remove(searchPanel);
                    searchGrid.Children.Add(searchPanel);
                }
            }
            else
            {
                if (searchGrid.Children.Contains(searchPanel))
                {
                    searchGrid.Children.Remove(searchPanel);
                    leftSlimPanel.Children.Add(searchPanel);
                }
            }
        }
    }
}
