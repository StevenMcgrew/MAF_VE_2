using MAF_VE_2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
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
        SQLite.Net.SQLiteConnection localDatabaseConnection;
        List<string> allCarMakes;

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
            InitializeLocalDatabase();

            allCarMakes = new List<string>();
        }

        void InitializeLocalDatabase()
        {
            // Connect to local database and create tables if they don't exist
            string localDatabasePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "MAFdatabase.sqlite");
            localDatabaseConnection = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), localDatabasePath);
            localDatabaseConnection.CreateTable<MAFcalculation>();
            localDatabaseConnection.CreateTable<LocalCarMake>();
        }

        private void mainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Insert engine combobox items
            double maxEngineSize = 9.0;
            double minEngineSize = 0.1;
            for (double i = minEngineSize; i <= maxEngineSize; i = i + 0.1)
            {
                string itemToAdd = i.ToString("0.0");
                engine.Items.Add(itemToAdd);
            }

            // Insert year combobox items
            int maxYear = (DateTime.Today.Year) + 2;
            int minYear = 1900;
            for (int i = maxYear; i >= minYear; i--)
            {
                year.Items.Add(i);
            }

            // Insert make combobox items
            RefreshMakesComboBox();
        }

#region Add or Delete a make

        private async void okAddMakeButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(addMakeTextBox.Text))
            {
                return;
            }
            else
            {
                string makeToAdd = addMakeTextBox.Text;

                // Check if item is already in the combobox
                var listOfItems = make.Items.Cast<string>().ToList();
                if (listOfItems.Contains(makeToAdd))
                {
                    await new MessageDialog("That car make already exists in the list.").ShowAsync();
                }
                else
                {
                    // Add new make to local database
                    localDatabaseConnection.Insert(new LocalCarMake()
                    {
                        Make = makeToAdd,
                    });

                    RefreshMakesComboBox();
                    addMakeTextBox.Text = "";
                    makeOptionsButton.Flyout.Hide();
                }
            }
        }

        private void cancelAddMakeButton_Click(object sender, RoutedEventArgs e)
        {
            addMakeTextBox.Text = "";
            makeOptionsButton.Flyout.Hide();
        }

        private void makeOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            addMakeButton.IsEnabled = true;

            var count = localDatabaseConnection.GetTableInfo("LocalCarMake").Count;
            if (count > 0)
            {
                deleteMakeButton.IsEnabled = true;
            }
            else
            {
                deleteMakeButton.IsEnabled = false;
            }
        }

        private void deleteMakeButton_Click(object sender, RoutedEventArgs e)
        {
            addMakeButton.IsEnabled = false;
            deleteMakeButton.IsEnabled = false;

            List<LocalCarMake> localCarMakes = localDatabaseConnection.Query<LocalCarMake>("SELECT * from LocalCarMake");
            MakesToDelete.ItemsSource = localCarMakes;
        }

        private void cancelDeleteMake_Click(object sender, RoutedEventArgs e)
        {
            makeOptionsButton.Flyout.Hide();
        }

        private void deleteMake_Click(object sender, RoutedEventArgs e)
        {
            var selectedMake = MakesToDelete.SelectedItem;
            var LocalMake = selectedMake as LocalCarMake;
            localDatabaseConnection.Delete<LocalCarMake>(LocalMake.ID);

            List<LocalCarMake> localCarMakes = localDatabaseConnection.Query<LocalCarMake>("SELECT * from LocalCarMake");
            MakesToDelete.ItemsSource = localCarMakes;

            RefreshMakesComboBox();
        }

        private void addMakeButton_Click(object sender, RoutedEventArgs e)
        {
            addMakeButton.IsEnabled = false;
            deleteMakeButton.IsEnabled = false;
        }

        // Functions/Methods /////////////////////////////////////////////////

        void RefreshMakesComboBox()
        {
            allCarMakes = null;

            // Get standard car makes and add to allCarMakes
            CarMakes carMakes = new CarMakes();
            allCarMakes = carMakes.StandardMakes;

            // Get makes from local database and add to allCarMakes and set ComboBox source
            List<LocalCarMake> localCarMakes = localDatabaseConnection.Query<LocalCarMake>("SELECT * from LocalCarMake");
            if (localCarMakes != null)
            {
                foreach (var car in localCarMakes)
                {
                    var c = car.Make.ToString();
                    allCarMakes.Add(c);
                }
            }
            allCarMakes.Sort();
            make.ItemsSource = allCarMakes;
        }

#endregion

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

        // Functions/Methods for SizeChanged /////////////////////////////////////////////////

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

#region Calculate

        private void calculateButton_Click(object sender, RoutedEventArgs e)
        {
            CalculateMafAndVe();
        }

        // Functions/Methods for Calculate //////////////////////////////////////////////

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
            decimal ExpectedMAF;
            decimal PercentDiff;
            decimal VolumetricEfficiency;

            try
            {
                // Textbox Text to Decimal
                RPM = Convert.ToDecimal(rpm.Text);
                MAF = Convert.ToDecimal(maf.Text);
                Liters = Convert.ToDecimal(engineSize.Text);
                Temperature = Convert.ToDecimal(airTemp.Text);
                Altitude = Convert.ToDecimal(altitude.Text);

                // Units conversion based on user choices
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

                //
                CalculatedBARO = SetCalculatedBARObyAltitude(Altitude);

                // Perform calculations to arrive at VolumePerSecond
                TemperatureK = (Temperature + 459.67M) * 5M / 9M;
                PressurePa = 3386.39M * CalculatedBARO;
                AirMassGramsPerLiter = PressurePa / (GasConstantForAir * TemperatureK);
                VolumePerSecond = (Liters * AirMassGramsPerLiter * RPM * 0.5M) / 60M;

                //
                ExpectedMAF = SetExpectedMAF(RPM, VolumePerSecond);

                // Set PercentDiff of MAF grams/sec and set VolumetricEfficiency
                PercentDiff = ((MAF - ExpectedMAF) / ExpectedMAF) * 100M;
                VolumetricEfficiency = (MAF / VolumePerSecond) * 100M;

                // Round values so they are display friendly
                PercentDiff = Math.Round(PercentDiff, 1);
                VolumetricEfficiency = Math.Round(VolumetricEfficiency, 1);
                ExpectedMAF = Math.Round(ExpectedMAF, 1);

                // Set Textbox Text for display
                expectedMAF.Text = Convert.ToString(ExpectedMAF);
                mafDifference.Text = Convert.ToString(PercentDiff);
                VE.Text = Convert.ToString(VolumetricEfficiency);

                // Set color of Textbox backgrounds if needed
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

                //
                TrySelectComboboxItemFromString(engine, engineSize.Text);
            }
            catch
            {
                await new MessageDialog("Cannot perform calculation. Input requirements: Numbers only; Only one decimal (or no decimals) per input box; No blank input boxes.").ShowAsync();
            }
        }

        void TrySelectComboboxItemFromString(ComboBox _comboBox, string _string)
        {
            try
            {
                string[] enginesArray = _comboBox.Items.Cast<Object>().Select(item => item.ToString()).ToArray();
                string matchingString = Array.Find(enginesArray, p => p.StartsWith(_string));
                _comboBox.SelectedItem = matchingString;
            }
            catch
            {
                _comboBox.SelectedIndex = 0;
            }
        }

        decimal SetExpectedMAF(decimal _rpm, decimal _volumePerSecond)
        {
            decimal expectedMaf;

            if (_rpm < 3100)
            {
                return expectedMaf = _volumePerSecond * 0.77M;
            }
            else if (_rpm >= 3100 && _rpm <= 3800)
            {
                return expectedMaf = _volumePerSecond * 0.84M;
            }
            else  // _rpm > 3800
            {
                return expectedMaf = _volumePerSecond * 0.82M;
            }
        }

        decimal SetCalculatedBARObyAltitude(decimal altitude)
        {
            decimal calculatedBaro;

            if (altitude < 3000)
            {
                return calculatedBaro = 29.92M - (altitude * 0.00104M);
            }
            else
            {
                return calculatedBaro = 29.92M - (altitude * 0.00101M);
            }
        }

        #endregion

#region Calculator TextChanged and SelectionChanged

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearCalculationResults();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearCalculationResults();
        }

        // Functions/Methods for TextChanged and SelectionChanged /////////////////////////////

        void ClearCalculationResults()
        {
            if (expectedMAF != null)
            {
                if (expectedMAF.Text == "")
                {
                    return;
                }
                else
                {
                    expectedMAF.Text = "";
                    mafDifference.Text = "";
                    VE.Text = "";
                    VE.Background = new SolidColorBrush(Colors.White);
                    mafDifference.Background = new SolidColorBrush(Colors.White);
                }
            }
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

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            altitude.ClearValue(TextBox.TextProperty);
            rpm.ClearValue(TextBox.TextProperty);
            engineSize.ClearValue(TextBox.TextProperty);
            maf.ClearValue(TextBox.TextProperty);
            airTemp.ClearValue(TextBox.TextProperty);
            VE.Background = new SolidColorBrush(Colors.White);
            mafDifference.Background = new SolidColorBrush(Colors.White);
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            year.SelectedIndex = -1;
            make.SelectedIndex = -1;
            model.Text = "";
            engine.SelectedIndex = -1;
            good.IsChecked = false;
            bad.IsChecked = false;
            unsure.IsChecked = false;
        }
    }
}