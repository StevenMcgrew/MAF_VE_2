using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
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

#region Enums

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

#endregion

        public MainPage()
        {
            InitializeComponent();
        }

#region SizeChanged handling

        private void mainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var pageSize = GetPageSize(mainPage);
            RearrangePanels(pageSize);

            var viewState = GetViewState();
            ManageMenuItems(viewState);
        }

        private void mainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewState = GetViewState();
            ManageMenuItems(viewState);
        }

        // Supporting functions/methods for SizeChanged /////////////////////////////////////////////////

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

        #endregion

#region Pivot navigation

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

#endregion



        private void printMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void shareMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void helpMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        async void CalculateMafAndVe()
        {
            decimal GasConstantForAir = 287.05M;
            decimal RPM;
            decimal MAF;
            decimal Liters;
            decimal Temperature;
            decimal Altitude;
            decimal CalculatedBARO;
            decimal TemperatureK;
            decimal PressurePa;
            decimal AirMassGramsPerLiter;
            decimal VolumePerSecond;
            decimal ExpectedMAF = 1;
            decimal PercentDiff;
            decimal VolumetricEfficiency;

            try
            {
                RPM = Convert.ToDecimal(rpm.Text);
                MAF = Convert.ToDecimal(maf.Text);
                Liters = Convert.ToDecimal(engineSize.Text);
                Temperature = Convert.ToDecimal(airTemp.Text);
                Altitude = Convert.ToDecimal(altitude.Text);

                if (mafUnits.SelectedIndex == 1)
                {
                    MAF = MAF / 3.6M;
                }

                if (airTempUnits.SelectedIndex == 1)
                {
                    Temperature = (Temperature * 1.8M) + 32M;
                }

                if (altitudeUnits.SelectedIndex == 1)
                {
                    Altitude = Altitude * 3.2808M;
                }

                if (Altitude < 3000)
                {
                    CalculatedBARO = 29.92M - (Altitude * 0.00104M);
                }
                else
                {
                    CalculatedBARO = 29.92M - (Altitude * 0.00101M);
                }

                TemperatureK = (Temperature + 459.67M) * 5M / 9M;
                PressurePa = 3386.39M * CalculatedBARO;
                AirMassGramsPerLiter = PressurePa / (GasConstantForAir * TemperatureK);
                VolumePerSecond = (Liters * AirMassGramsPerLiter * RPM * 0.5M) / 60M;

                if (RPM < 3100)
                {
                    ExpectedMAF = VolumePerSecond * 0.77M;
                }
                else if (RPM >= 3100 && RPM <= 3800)
                {
                    ExpectedMAF = VolumePerSecond * 0.84M;
                }
                else if (RPM > 3800)
                {
                    ExpectedMAF = VolumePerSecond * 0.82M;
                }

                PercentDiff = ((MAF - ExpectedMAF) / ExpectedMAF) * 100M;
                VolumetricEfficiency = (MAF / VolumePerSecond) * 100M;

                PercentDiff = Math.Round(PercentDiff, 1);
                VolumetricEfficiency = Math.Round(VolumetricEfficiency, 1);
                ExpectedMAF = Math.Round(ExpectedMAF, 1);

                expectedMAF.Text = Convert.ToString(ExpectedMAF);
                mafDifference.Text = Convert.ToString(PercentDiff);
                VE.Text = Convert.ToString(VolumetricEfficiency);

                if (PercentDiff <= -8)
                {
                    mafDifference.Background = new SolidColorBrush(Colors.Orange);
                }

                if (PercentDiff <= -12)
                {
                    mafDifference.Background = new SolidColorBrush(Colors.Red);
                }

                if (VolumetricEfficiency <= 75)
                {
                    VE.Background = new SolidColorBrush(Colors.Orange);
                }

                if (VolumetricEfficiency <= 72)
                {
                    VE.Background = new SolidColorBrush(Colors.Red);
                }

                try
                {
                    //Compare engine size entered to combobox items, then set combobox to match
                    string engineString = engineSize.Text;
                    string[] enginesArray = engine.Items.Cast<Object>().Select(item => item.ToString()).ToArray();
                    string matchingString = Array.Find(enginesArray, p => p.StartsWith(engineString));
                    engine.SelectedItem = matchingString;
                }
                catch
                {
                    
                }
            }
            catch
            {
                await new MessageDialog("Cannot perform calculation. Input requirements: Numbers only; Only one decimal (or no decimals) per input box; No blank input boxes.").ShowAsync();
            }
        }

        private void calculateButton_Click(object sender, RoutedEventArgs e)
        {
            CalculateMafAndVe();
        }

        private void mainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //Insert Items
            #region
            string[] engines = new string[] { "0.5L", "0.6L", "0.7L", "0.8L", "0.9L",
                                              "1.0L", "1.1L", "1.2L", "1.3L", "1.4L", "1.5L", "1.6L", "1.7L", "1.8L", "1.9L",
                                              "2.0L", "2.1L", "2.2L", "2.3L", "2.4L", "2.5L", "2.6L", "2.7L", "2.8L", "2.9L",
                                              "3.0L", "3.1L", "3.2L", "3.3L", "3.4L", "3.5L", "3.6L", "3.7L", "3.8L", "3.9L",
                                              "4.0L", "4.1L", "4.2L", "4.3L", "4.4L", "4.5L", "4.6L", "4.7L", "4.8L", "4.9L",
                                              "5.0L", "5.1L", "5.2L", "5.3L", "5.4L", "5.5L", "5.6L", "5.7L", "5.8L", "5.9L",
                                              "6.0L", "6.1L", "6.2L", "6.3L", "6.4L", "6.5L", "6.6L", "6.7L", "6.8L", "6.9L",
                                              "7.0L", "7.1L", "7.2L", "7.3L", "7.4L", "7.5L", "7.6L", "7.7L", "7.8L", "7.9L",
                                              "8.0L", "8.1L", "8.2L", "8.3L", "8.4L" };

            int engineCount = engines.Length;
            int h;
            for (h = engineCount - 1; h > (-1); h--)
            {
                engine.Items.Insert(0, engines[h]);
            }
            #endregion
        }
    }
}
