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

        enum BasicViewState
        {
            Wide,
            NarrowCalculator,
            NarrowDatabase,
            ShortCalculator,
            ShortSaveOrSearch,
            ShortDatabase
        };

        private void mainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var basicViewState = GetBasicViewState();
            RearrangePanels(basicViewState);
            ManageMenuItems(basicViewState);


            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //if (mainPage.ActualWidth < 640)
            //{


            //    if (mainGrid.Children.Contains(recordsPanel))
            //    {
            //        mainGrid.Children.Remove(recordsPanel);
            //        databaseGrid.Children.Add(recordsPanel);
            //        mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            //        mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            //    }
            //}
            //else
            //{
            //    if (databaseGrid.Children.Contains(recordsPanel))
            //    {
            //        databaseGrid.Children.Remove(recordsPanel);
            //        mainGrid.Children.Add(recordsPanel);
            //        mainGrid.ColumnDefinitions[0].Width = GridLength.Auto;
            //        mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            //        mainPivot.SelectedItem = frontPivotItem;
            //    }
            //}

            //if ((mainPage.ActualHeight < 700) && (mainPage.ActualWidth < 640))
            //{
            //    if (leftSlimPanel.Children.Contains(searchPanel))
            //    {
            //        leftSlimPanel.Children.Remove(searchPanel);
            //        saveOrSearchGrid.Children.Add(searchPanel);
            //    }
            //}
            //else
            //{
            //    if (saveOrSearchGrid.Children.Contains(searchPanel))
            //    {
            //        saveOrSearchGrid.Children.Remove(searchPanel);
            //        leftSlimPanel.Children.Add(searchPanel);
            //        mainPivot.SelectedItem = frontPivotItem;
            //    }
            //}
        }

        BasicViewState GetBasicViewState()
        {
            if ((mainPage.ActualHeight < 700) && (mainPage.ActualWidth < 640))
            {
                if (mainPivot.SelectedItem == frontPivotItem)
                {
                    return BasicViewState.ShortCalculator;
                }
                else if (mainPivot.SelectedItem == saveOrSearchPivotItem)
                {
                    return BasicViewState.ShortSaveOrSearch;
                }
                else
                {
                    return BasicViewState.ShortDatabase;
                }
            }
            else if (mainPage.ActualWidth < 640)
            {
                if (mainPivot.SelectedItem == frontPivotItem)
                {
                    return BasicViewState.NarrowCalculator;
                }
                else
                {
                    return BasicViewState.NarrowDatabase;
                }
            }
            else
            {
                return BasicViewState.Wide;
            }
        }

        void ManageMenuItems(BasicViewState view)
        {
            switch (view)
            {
                case BasicViewState.Wide:
                    calculatorMenuItem.Visibility = Visibility.Collapsed;
                    saveOrSearchMenuItem.Visibility = Visibility.Collapsed;
                    databaseMenuItem.Visibility = Visibility.Collapsed;
                    break;

                case BasicViewState.NarrowCalculator:
                    calculatorMenuItem.Visibility = Visibility.Collapsed;
                    saveOrSearchMenuItem.Visibility = Visibility.Collapsed;
                    databaseMenuItem.Visibility = Visibility.Visible;
                    break;

                case BasicViewState.NarrowDatabase:
                    calculatorMenuItem.Visibility = Visibility.Visible;
                    saveOrSearchMenuItem.Visibility = Visibility.Visible;
                    databaseMenuItem.Visibility = Visibility.Collapsed;
                    break;

                case BasicViewState.ShortCalculator:
                    calculatorMenuItem.Visibility = Visibility.Collapsed;
                    saveOrSearchMenuItem.Visibility = Visibility.Visible;
                    databaseMenuItem.Visibility = Visibility.Visible;
                    break;

                case BasicViewState.ShortSaveOrSearch:
                    calculatorMenuItem.Visibility = Visibility.Visible;
                    saveOrSearchMenuItem.Visibility = Visibility.Collapsed;
                    databaseMenuItem.Visibility = Visibility.Visible;
                    break;

                case BasicViewState.ShortDatabase:
                    calculatorMenuItem.Visibility = Visibility.Visible;
                    saveOrSearchMenuItem.Visibility = Visibility.Visible;
                    databaseMenuItem.Visibility = Visibility.Collapsed;
                    break;

                default:
                    calculatorMenuItem.Visibility = Visibility.Visible;
                    saveOrSearchMenuItem.Visibility = Visibility.Visible;
                    databaseMenuItem.Visibility = Visibility.Visible;
                    break;
            }
        }

        void RearrangePanels(BasicViewState view)
        {
            switch (view)
            {
                case BasicViewState.Wide:
                    if (databaseGrid.Children.Contains(recordsPanel))
                    {
                        databaseGrid.Children.Remove(recordsPanel);
                        mainGrid.Children.Add(recordsPanel);
                        mainGrid.ColumnDefinitions[0].Width = GridLength.Auto;
                        mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                        mainPivot.SelectedItem = frontPivotItem;
                    }
                    if (saveOrSearchGrid.Children.Contains(searchPanel))
                    {
                        saveOrSearchGrid.Children.Remove(searchPanel);
                        leftSlimPanel.Children.Add(searchPanel);
                        mainPivot.SelectedItem = frontPivotItem;
                    }
                    break;

                case BasicViewState.NarrowCalculator:
                    if (mainGrid.Children.Contains(recordsPanel))
                    {
                        mainGrid.Children.Remove(recordsPanel);
                        databaseGrid.Children.Add(recordsPanel);
                        mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                        mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                    }
                    if (saveOrSearchGrid.Children.Contains(searchPanel))
                    {
                        saveOrSearchGrid.Children.Remove(searchPanel);
                        leftSlimPanel.Children.Add(searchPanel);
                        mainPivot.SelectedItem = frontPivotItem;
                    }
                    break;

                case BasicViewState.NarrowDatabase:
                    if (saveOrSearchGrid.Children.Contains(searchPanel))
                    {
                        saveOrSearchGrid.Children.Remove(searchPanel);
                        leftSlimPanel.Children.Add(searchPanel);
                        mainPivot.SelectedItem = frontPivotItem;
                    }
                    break;

                case BasicViewState.ShortCalculator:
                    if (leftSlimPanel.Children.Contains(searchPanel))
                    {
                        leftSlimPanel.Children.Remove(searchPanel);
                        saveOrSearchGrid.Children.Add(searchPanel);
                    }
                    break;
                case BasicViewState.ShortSaveOrSearch:
                    break;
                case BasicViewState.ShortDatabase:
                    break;
                default:
                    break;
            }
        }

        private void calculatorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            mainPivot.SelectedItem = frontPivotItem;
        }

        private void saveOrSearchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            mainPivot.SelectedItem = saveOrSearchPivotItem;
        }

        private void databaseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            mainPivot.SelectedItem = databasePivotItem;
        }

        private void printMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void shareMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void helpMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var basicViewState = GetBasicViewState();
            ManageMenuItems(basicViewState);
        }
    }
}
