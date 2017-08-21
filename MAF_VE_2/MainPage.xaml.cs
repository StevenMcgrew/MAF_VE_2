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
        enum ViewState
        {
            Wide,
            NarrowCalculator,
            NarrowDatabase,
            ShortCalculator,
            ShortSaveOrSearch,
            ShortDatabase
        };

        enum PageSize
        {
            Wide,
            Narrow,
            Short
        }

        public MainPage()
        {
            InitializeComponent();
        }

        private void mainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var pageSize = GetPageSize(mainPage);
            RearrangePanels(pageSize);

            var viewState = GetViewState();
            ManageMenuItems(viewState);
        }

        PageSize GetPageSize(Page page)
        {
            if ((page.ActualHeight < 700) && (page.ActualWidth < 640))
            {
                return PageSize.Short;
            }
            else if ((page.ActualWidth < 640) && (page.ActualHeight >=700))
            {
                return PageSize.Narrow;
            }
            else
            {
                return PageSize.Wide;
            }
        }

        void RearrangePanels(PageSize pageSize)
        {
            switch (pageSize)
            {
                case PageSize.Wide:
                    if (mainPivot.SelectedItem == frontPivotItem)
                    {
                        MoveRecordsToFront();
                    }
                    else if (mainPivot.SelectedItem == saveOrSearchPivotItem)
                    {
                        MoveSearchToFront();
                        MoveRecordsToFront();
                        mainPivot.SelectedItem = frontPivotItem;
                    }
                    else if (mainPivot.SelectedItem == databasePivotItem)
                    {
                        MoveRecordsToFront();
                        mainPivot.SelectedItem = frontPivotItem;
                    }
                    break;

                case PageSize.Narrow:
                    if (mainPivot.SelectedItem == frontPivotItem)
                    {
                        MoveRecordsToPivotItem();
                        MoveSearchToFront();
                    }
                    else if (mainPivot.SelectedItem == saveOrSearchPivotItem)
                    {
                        MoveSearchToFront();
                        mainPivot.SelectedItem = frontPivotItem;
                    }
                    else if (mainPivot.SelectedItem == databasePivotItem)
                    {
                        MoveSearchToFront();
                    }
                    break;

                case PageSize.Short:
                    if (mainPivot.SelectedItem == frontPivotItem)
                    {
                        MoveRecordsToPivotItem();
                        MoveSearchToPivotItem();
                    }
                    //else if (mainPivot.SelectedItem == saveOrSearchPivotItem)
                    //{

                    //}
                    //else if (mainPivot.SelectedItem == databasePivotItem)
                    //{

                    //}
                    break;

                default:
                    MoveSearchToFront();
                    MoveRecordsToFront();
                    mainPivot.SelectedItem = frontPivotItem;
                    break;
            }
        }

        void MoveSearchToPivotItem()
        {
            if (leftSlimPanel.Children.Contains(searchPanel))
            {
                leftSlimPanel.Children.Remove(searchPanel);
                saveOrSearchGrid.Children.Add(searchPanel);
            }
        }

        void MoveSearchToFront()
        {
            if (saveOrSearchGrid.Children.Contains(searchPanel))
            {
                saveOrSearchGrid.Children.Remove(searchPanel);
                leftSlimPanel.Children.Add(searchPanel);
            }
        }

        void MoveRecordsToPivotItem()
        {
            if (mainGrid.Children.Contains(recordsPanel))
            {
                mainGrid.Children.Remove(recordsPanel);
                databaseGrid.Children.Add(recordsPanel);
                mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            }
        }

        void MoveRecordsToFront()
        {
            if (databaseGrid.Children.Contains(recordsPanel))
            {
                databaseGrid.Children.Remove(recordsPanel);
                mainGrid.Children.Add(recordsPanel);
                mainGrid.ColumnDefinitions[0].Width = GridLength.Auto;
                mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            }
        }

        ViewState GetViewState()
        {
            var pageSize = GetPageSize(mainPage);

            if (pageSize == PageSize.Short)
            {
                if (mainPivot.SelectedItem == frontPivotItem)
                {
                    return ViewState.ShortCalculator;
                }
                else if (mainPivot.SelectedItem == saveOrSearchPivotItem)
                {
                    return ViewState.ShortSaveOrSearch;
                }
                else
                {
                    return ViewState.ShortDatabase;
                }
            }
            else if (pageSize == PageSize.Narrow)
            {
                if (mainPivot.SelectedItem == frontPivotItem)
                {
                    return ViewState.NarrowCalculator;
                }
                else
                {
                    return ViewState.NarrowDatabase;
                }
            }
            else
            {
                return ViewState.Wide;
            }
        }

        void ManageMenuItems(ViewState view)
        {
            switch (view)
            {
                case ViewState.Wide:
                    calculatorMenuItem.IsEnabled = false;
                    saveOrSearchMenuItem.IsEnabled = false;
                    databaseMenuItem.IsEnabled = false;
                    break;

                case ViewState.NarrowCalculator:
                    calculatorMenuItem.IsEnabled = false;
                    saveOrSearchMenuItem.IsEnabled = false;
                    databaseMenuItem.IsEnabled = true;
                    break;

                case ViewState.NarrowDatabase:
                    calculatorMenuItem.IsEnabled = true;
                    saveOrSearchMenuItem.IsEnabled = true;
                    databaseMenuItem.IsEnabled = false;
                    break;

                case ViewState.ShortCalculator:
                    calculatorMenuItem.IsEnabled = false;
                    saveOrSearchMenuItem.IsEnabled = true;
                    databaseMenuItem.IsEnabled = true;
                    break;

                case ViewState.ShortSaveOrSearch:
                    calculatorMenuItem.IsEnabled = true;
                    saveOrSearchMenuItem.IsEnabled = false;
                    databaseMenuItem.IsEnabled = true;
                    break;

                case ViewState.ShortDatabase:
                    calculatorMenuItem.IsEnabled = true;
                    saveOrSearchMenuItem.IsEnabled = true;
                    databaseMenuItem.IsEnabled = false;
                    break;

                default:
                    calculatorMenuItem.IsEnabled = true;
                    saveOrSearchMenuItem.IsEnabled = true;
                    databaseMenuItem.IsEnabled = true;
                    break;
            }
        }

        private void calculatorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            mainPivot.SelectedItem = frontPivotItem;
        }

        private void saveOrSearchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewState = GetViewState();

            if (viewState == ViewState.NarrowDatabase)
            {
                mainPivot.SelectedItem = frontPivotItem;
            }
            else
            {
                mainPivot.SelectedItem = saveOrSearchPivotItem;
            }
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
            var viewState = GetViewState();
            ManageMenuItems(viewState);
        }
    }
}
