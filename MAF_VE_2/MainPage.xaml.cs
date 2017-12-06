using MAF_VE_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Windows.Web.Http;

namespace MAF_VE_2
{
    public sealed partial class MainPage : Page
    {

        #region General variables and fields

        SQLite.Net.SQLiteConnection localDatabaseConnection;
        List<string> allCarMakes;
        bool rbCheckFired = false;
        string condition = "";
        bool downloadedImage = false;

        ApplicationDataContainer localSettings = null;
        const string DateOfLastImageDownloadAsSum = "DateOfLastImageDownload";
        const string BackgroundImageSetting = "BackgroundImageSetting";

        #endregion

        #region Variables for charts

        double lowRPM;
        double highRPM;
        double lowMAF;
        double highMAF;
        double lowVE;
        double highVE;
        double rpmPerPixel;
        double mafPerPixel;
        double vePerPixel;

        #endregion

        #region Variables For Printing

        //private PrintManager printMan;
        //private PrintDocument printDoc;
        //private IPrintDocumentSource printDocSource;

        #endregion

        #region Startup and Initialization

        public MainPage()
        {
            InitializeComponent();
            InitializeLocalDatabase();

            allCarMakes = new List<string>();

            // Initialize Settings storage
            localSettings = ApplicationData.Current.LocalSettings;
        }

        void InitializeLocalDatabase()
        {
            // Connect to local database and create tables if they don't exist
            string localDatabasePath = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "MAFdatabase.sqlite");
            localDatabaseConnection = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), localDatabasePath);
            localDatabaseConnection.CreateTable<MAFcalculation>();
            localDatabaseConnection.CreateTable<LocalCarMake>();
        }

        private void mainPage_Loaded(object sender, RoutedEventArgs e)
        {
            AddEngineComboboxItems();
            AddYearComboboxItems();
            RefreshMakesComboBox();
            ShowAllLocalRecords();

            // Handle background image setting
            Object backgroundImageSetting = localSettings.Values[BackgroundImageSetting];
            if (backgroundImageSetting != null)
            {
                var backgroundSetting = (bool)backgroundImageSetting;
                if (backgroundSetting == true)
                {
                    yesImage.IsChecked = true;
                }
                else
                {
                    noImage.IsChecked = true;
                }
            }
            else
            {
                yesImage.IsChecked = true;
            }

            DownloadImageIfNew();
        }

        void AddEngineComboboxItems()
        {
            engine.Items.Add("Select");  // Add this first so the user has a way to escape having to select something

            double maxEngineSize = 9.0;
            double minEngineSize = 0.1;
            for (double i = minEngineSize; i <= maxEngineSize; i = i + 0.1)
            {
                string itemToAdd = i.ToString("0.0") + "L";
                engine.Items.Add(itemToAdd);
            }
        }

        void AddYearComboboxItems()
        {
            // Add this first so the user has a way to escape having to select something
            year.Items.Add("Select");

            // Variables
            var localSettings = ApplicationData.Current.LocalSettings;
            int currentMaxYear = DateTime.Today.Year + 2;
            int storedMaxYear;
            int minYear = 1900;

            // Set storedMaxYear based on whether or not a value has been saved in LocalSettings
            if (localSettings.Values["maxYear"] != null)
            {
                storedMaxYear = (int)localSettings.Values["maxYear"];
            }
            else
            {
                localSettings.Values["maxYear"] = currentMaxYear;
                storedMaxYear = currentMaxYear;
            }

            // Update LocalSettings and storedMaxYear if less than currentMaxYear
            if (storedMaxYear < currentMaxYear)
            {
                localSettings.Values["maxYear"] = currentMaxYear;
                storedMaxYear = currentMaxYear;
            }

            // Add the years to the combobox
            for (int i = storedMaxYear; i >= minYear; i--)
            {
                year.Items.Add(i);
            }
        }

        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    // Register for PrintTaskRequested event
        //    printMan = PrintManager.GetForCurrentView();
        //    printMan.PrintTaskRequested += PrintTaskRequested;

        //    // Build a PrintDocument and register for callbacks
        //    printDoc = new PrintDocument();
        //    printDocSource = printDoc.DocumentSource;
        //    printDoc.Paginate += Paginate;
        //    printDoc.GetPreviewPage += GetPreviewPage;
        //    printDoc.AddPages += AddPages;

        //    // Register the current page as a share source.
        //    dataTransferManager = DataTransferManager.GetForCurrentView();
        //    dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareImageHandler);

        //    // For shortcut keys
        //    Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        //}

        //protected override void OnNavigatedFrom(NavigationEventArgs e)
        //{
        //    dataTransferManager.DataRequested -= ShareImageHandler;
        //    printMan.PrintTaskRequested -= PrintTaskRequested;
        //    printDoc.Paginate -= Paginate;
        //    printDoc.GetPreviewPage -= GetPreviewPage;
        //    printDoc.AddPages -= AddPages;
        //    Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
        //}

        #endregion

        #region Bing image of the day

        async void SetBackgroundImage()
        {
            // Set background image first
            try
            {
                var imageFile = await ApplicationData.Current.LocalFolder.GetFileAsync("BingImageOfTheDay.jpg");
                using (var stream = await imageFile.OpenReadAsync())
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);
                    backgroundImage.Source = bitmapImage;
                }
            }
            catch
            {
                backgroundImage.Source = new BitmapImage(new Uri(BaseUri, "/Assets/hdBackground.png"));
                return;
            }

            // Then set copyright text
            bool copyrightSetSuccessfully = false;
            try
            {
                var copyrightFile = await ApplicationData.Current.LocalFolder.GetFileAsync("Copyright.txt");
                var copyrightText = await FileIO.ReadTextAsync(copyrightFile);
                if (downloadedImage)
                {
                    copyright.Text = "Image of the day: " + copyrightText;
                }
                else
                {
                    copyright.Text = "Image: " + copyrightText;
                }
                copyrightSetSuccessfully = true;
                copyrightButton.BorderThickness = new Thickness(1);
            }
            catch
            {
                copyright.Text = "Image: Could not get copyright info";
                copyrightButton.BorderThickness = new Thickness(1);
                copyrightSetSuccessfully = false;
            }
            finally
            {
                if (copyrightSetSuccessfully)
                {
                    downloadedImage = false; // reset this to false for the next time around
                }
            }
        }

        async void DownloadImageIfNew()
        {
            string region = "en-US";
            int numberOfImages = 1;
            string bingImageURL = string.Format("http://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n={0}&mkt={1}", numberOfImages, region);
            string JSON = "";

            try // Get JSON
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage httpResponse = await httpClient.GetAsync(new Uri(bingImageURL));
                    JSON = await httpResponse.Content.ReadAsStringAsync();
                }
            }
            catch
            {
                return;
            }

            try // Parse JSON
            {
                JsonObject jsonObject;
                bool IsParsed = JsonObject.TryParse(JSON, out jsonObject);
                Uri bingUri;
                if (IsParsed)
                {
                    string partialUrlForImage = jsonObject["images"].GetArray()[0].GetObject()["url"].GetString();
                    string copyrightText = jsonObject["images"].GetArray()[0].GetObject()["copyright"].GetString();

                    // Check if new image by comparing copyright text
                    StorageFile savedCopyrightFile;
                    string savedCopyrightText = "none";
                    try
                    {
                        savedCopyrightFile = await ApplicationData.Current.LocalFolder.GetFileAsync("Copyright.txt");
                        savedCopyrightText = await FileIO.ReadTextAsync(savedCopyrightFile);
                    }
                    catch
                    {
                        // No file. An image has not been downloaded before.
                    }

                    if (copyrightText == savedCopyrightText)
                    {
                        return; // same image as last time, so don't download again
                    }
                    else
                    {
                        string completeUrlForImage = "https://www.bing.com" + partialUrlForImage;
                        bingUri = new Uri(completeUrlForImage);

                        // Save image
                        string fileName = "BingImageOfTheDay.jpg";
                        RandomAccessStreamReference IRASRstream = RandomAccessStreamReference.CreateFromUri(bingUri);
                        StorageFile remoteFile = await StorageFile.CreateStreamedFileFromUriAsync(fileName, bingUri, IRASRstream);
                        await remoteFile.CopyAsync(ApplicationData.Current.LocalFolder, fileName, NameCollisionOption.ReplaceExisting);

                        // Save copyright
                        StorageFile copyrightFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Copyright.txt", CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteTextAsync(copyrightFile, copyrightText);

                        downloadedImage = true;
                    }
                }
            }
            catch
            {
                return;
            }

            // Set the background image
            SetBackgroundImage();
        }

        private void copyrightButton_Click(object sender, RoutedEventArgs e)
        {
            mainPivotFadeOutStory.Begin();
            menuButton.Opacity = 0.00;
            mainPivot.IsHitTestVisible = false;
            menuButton.IsHitTestVisible = false;
            copyright2.Text = copyright.Text;
            imageSavePanel.Visibility = Visibility.Visible;
        }

        private void closeImageSaverButton_Click(object sender, RoutedEventArgs e)
        {
            mainPivotFadeInStory.Begin();
            menuButton.Opacity = 1.00;
            mainPivot.IsHitTestVisible = true;
            menuButton.IsHitTestVisible = true;
            imageSavePanel.Visibility = Visibility.Collapsed;
        }

        private async void saveImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Get image file from LocalFolder
            StorageFile imageFile;
            try
            {
                imageFile = await ApplicationData.Current.LocalFolder.GetFileAsync("BingImageOfTheDay.jpg");
            }
            catch
            {
                await new MessageDialog("Sorry, there was a problem getting the image file.").ShowAsync();
                return;
            }

            // Let user save image with FileSavePicker
            try
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                savePicker.SuggestedFileName = await CreateFileNameFromCopyrightInfo();
                savePicker.FileTypeChoices.Add(".jpg Joint Photographic Experts Group", new List<string>() { ".jpg" });
                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    await imageFile.CopyAndReplaceAsync(file);
                    savedPopupStory.Begin();
                }
            }
            catch
            {
                await new MessageDialog("Sorry, there was a problem saving the image file.").ShowAsync();
            }
        }

        async Task<string> CreateFileNameFromCopyrightInfo()
        {
            string fileName = "Bing_image_of_the_day";
            try
            {
                // Get and set initial fileName
                StorageFile copyrightFile = await ApplicationData.Current.LocalFolder.GetFileAsync("Copyright.txt");
                string copyrightText = await FileIO.ReadTextAsync(copyrightFile);
                fileName = copyrightText;

                // Remove copyright info from the end of the string
                int indexOfChar = fileName.IndexOf('(');
                if (indexOfChar > 1)
                {
                    fileName = fileName.Remove(indexOfChar - 1);
                }

                // Rebuild string with common characters only
                StringBuilder sb = new StringBuilder();
                foreach (char c in fileName)
                {
                    if ((c >= '0' && c <= '9') ||
                        (c >= 'A' && c <= 'Z') ||
                        (c >= 'a' && c <= 'z') ||
                        (c == ' '))
                    {
                        sb.Append(c);
                    }
                }
                fileName = sb.ToString();

                // Replace spaces with underscores.
                fileName = fileName.Replace(' ', '_');
            }
            catch
            {
                fileName = "Bing_image_of_the_day";
            }

            return fileName;
        }

        #endregion

        #region Databases

        void ShowAllLocalRecords()
        {
            var mafRecords = localDatabaseConnection.Query<MAFcalculation>("SELECT * from MAFcalculation ORDER BY vehicleID DESC");
            localRecords.ItemsSource = mafRecords;
            if (mafRecords.Count == 0)
            {
                noResults.Visibility = Visibility.Visible;
                mafChartNoResultsLabel.Visibility = Visibility.Visible;
                veChartNoResultsLabel.Visibility = Visibility.Visible;
            }
            else
            {
                noResults.Visibility = Visibility.Collapsed;
                mafChartNoResultsLabel.Visibility = Visibility.Collapsed;
                veChartNoResultsLabel.Visibility = Visibility.Collapsed;
            }

            searchedForText.Text = "all records";
            veChartDataDescription.Text = "Perform a search to see data in this chart";
            mafChartDataDescription.Text = "Perform a search to see data in this chart";
            ClearChartData();
        }

        #endregion

        #region Unchecking, Unselecting, Selecting 

        private void ConditionRadioButtons_Click(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;

            if (rbCheckFired) // The radiobutton was just checked, so do nothing except set rbCheckFired back to false for the next time we need to evaluate this scenario.
            {
                rbCheckFired = false;
            }
            else // The radiobutton checked event was NOT fired, which means the radiobutton was already checked, so we want to uncheck it.
            {
                radioButton.IsChecked = false;
                condition = "";
            }
        }

        private void ConditionRadioButtons_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            condition = radioButton.Content.ToString();

            rbCheckFired = true;
        }

        private void YearOrMake_DropDownOpened(object sender, object e)
        {
            var comboBox = sender as ComboBox;

            if (comboBox.SelectedIndex == -1) // Nothing has been selected yet
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private void YearMakeOrEngine_DropDownClosed(object sender, object e)
        {
            var comboBox = sender as ComboBox;

            if (comboBox.SelectedIndex == -1)  // No item is selected.........Index -1 is the unselected state where the Placeholder text shows, so exit this event since we don't need to do anything. This also prevents reevaluating the selection when we set the selection here (breaks the loop).
            {
                return;
            }

            var selectedItem = comboBox.SelectedItem.ToString();

            if (selectedItem == "Select")
            {
                comboBox.SelectedIndex = -1;
            }
        }

        #endregion

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

            List<LocalCarMake> localCarMakes = localDatabaseConnection.Query<LocalCarMake>("SELECT * from LocalCarMake");
            if (localCarMakes.Count > 0)
            {
                deleteMakeButton.IsEnabled = true;
                MakesToDelete.ItemsSource = localCarMakes;
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
        }

        private void cancelDeleteMake_Click(object sender, RoutedEventArgs e)
        {
            makeOptionsButton.Flyout.Hide();
        }

        private void deleteMake_Click(object sender, RoutedEventArgs e)
        {
            var selectedMake = MakesToDelete.SelectedItem;

            if (selectedMake != null)
            {
                var LocalMake = selectedMake as LocalCarMake;
                localDatabaseConnection.Delete<LocalCarMake>(LocalMake.ID);

                List<LocalCarMake> localCarMakes = localDatabaseConnection.Query<LocalCarMake>("SELECT * from LocalCarMake");
                MakesToDelete.ItemsSource = localCarMakes;

                RefreshMakesComboBox();
                deleteMake.IsEnabled = false;
            }
        }

        private void addMakeButton_Click(object sender, RoutedEventArgs e)
        {
            addMakeButton.IsEnabled = false;
            deleteMakeButton.IsEnabled = false;
        }

        private void MakesToDelete_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            deleteMake.IsEnabled = true;
        }

        private void deleteMakeFlyout_Closed(object sender, object e)
        {
            deleteMake.IsEnabled = false;
            makeOptionsButton.Flyout.Hide();
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
            allCarMakes.Insert(0, "Select"); // Insert this at the top of the list for users to have option to not select any car.
            make.ItemsSource = allCarMakes;
        }

        #endregion

        #region SizeChanged handling

        private void Charts_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double newWidth = e.NewSize.Width;

            if (newWidth < 700)
            {
                ChartsStackPanel.Orientation = Orientation.Vertical;
            }
            else if (newWidth > 700)
            {
                ChartsStackPanel.Orientation = Orientation.Horizontal;
            }
        }

        private void mainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (mainPage.ActualWidth < 660) // Mobile layout
            {
                MoveRecordsToPivotItem();
            }
            else // Desktop layout
            {
                MoveRecordsToFront();
                mainPivot.SelectedItem = frontPivotItem;
            }

            if (mainPage.ActualHeight < 560)
            {
                copyrightButton.Opacity = 0.0;
            }
            else
            {
                copyrightButton.Opacity = 1.0;
            }
        }

        // Functions/Methods for SizeChanged /////////////////////////////////////////////////

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

        #endregion

        #region Menu navigation

        private void calculatorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (mainPivot.SelectedItem == frontPivotItem)
            {
                CalculatorPanelColorStoryboard.Begin();
            }
            else
            {
                mainPivot.SelectedItem = frontPivotItem;
            }
        }

        private void databaseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (mainGrid.Children.Contains(recordsPanel)) // View is desktop
            {
                if (mainPivot.SelectedItem == helpPivotItem)
                {
                    mainPivot.SelectedItem = frontPivotItem;
                }
                else
                {
                    SearchPanelColorStoryboard.Begin();
                }
            }
            else // View is mobile
            {
                if (mainPivot.SelectedItem == databasePivotItem)
                {
                    SearchPanelColorStoryboard.Begin();
                }
                else
                {
                    mainPivot.SelectedItem = databasePivotItem;
                    recordsViewPivot.SelectedItem = Local;
                }
            }
        }

        private void helpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //Frame.Navigate(typeof(BlankPage1));
            mainPivot.SelectedItem = helpPivotItem;
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

        private void closeExpectedNoteButton_Click(object sender, RoutedEventArgs e)
        {
            expectedMafNoteButton.Flyout.Hide();
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

        #region Clear and Reset buttons

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
            condition = "";
            comments.ClearValue(TextBox.TextProperty);

            ShowAllLocalRecords();
        }

        #endregion

        #region Save

        private async void saveButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog1 = new MessageDialog("Must enter Year, Make, Model, Engine, and select Good/Bad/Unsure. Only the comments section is optional.");

            string _year;
            string _make;
            string _model;
            string _engine;
            string _condition;
            string _comments;

            try
            {
                _year = year.SelectedItem.ToString();
                _make = make.SelectedItem.ToString();
                _model = model.Text;
                _engine = engine.SelectedItem.ToString();
                _condition = condition;
                _comments = comments.Text;
            }
            catch
            {
                await dialog1.ShowAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(_model))
            {
                await dialog1.ShowAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(_condition))
            {
                await dialog1.ShowAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(_comments))
            {
                _comments = "No comments";
            }

            try
            {
                localDatabaseConnection.Insert(new MAFcalculation()
                {
                    Year = _year,
                    Make = _make,
                    Model = _model,
                    Engine = _engine,
                    Condition = _condition,
                    Comments = _comments,
                    Engine_speed = Convert.ToDouble(rpm.Text),
                    MAF = Convert.ToDouble(maf.Text),
                    Engine_size = Convert.ToDouble(engineSize.Text),
                    Air_temperature = Convert.ToDouble(airTemp.Text),
                    Altitude = Convert.ToDouble(altitude.Text),
                    Expected_MAF = Convert.ToDouble(expectedMAF.Text),
                    MAF_Difference = Convert.ToDouble(mafDifference.Text),
                    Volumetric_Efficiency = Convert.ToDouble(VE.Text),
                    MAF_units = mafUnits.SelectedValue.ToString(),
                    Temp_units = airTempUnits.SelectedValue.ToString(),
                    Altitude_units = altitudeUnits.SelectedValue.ToString()
                });
                
                ShowAllLocalRecords();
                savedPopupStory.Begin();

                good.ClearValue(RadioButton.IsCheckedProperty);
                bad.ClearValue(RadioButton.IsCheckedProperty);
                unsure.ClearValue(RadioButton.IsCheckedProperty);
                condition = "";
                comments.ClearValue(TextBox.TextProperty);
            }
            catch
            {
                await new MessageDialog("Failed to save. Make sure your inputs in the calculator are correct. Input requirements: Numbers only; Only one decimal (or no decimals) per input box; No blank input boxes.").ShowAsync();
            }
        }

        #endregion

        #region Record click

        private void localRecords_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as MAFcalculation;

            YMME.Text = item.Year + " " +
                        item.Make + " " +
                        item.Model + " " +
                        item.Engine;

            RPM.Text = item.Engine_speed.ToString();
            MAF.Text = item.MAF.ToString();
            MAF_UNITS.Text = item.MAF_units;
            AIR.Text = item.Air_temperature.ToString();
            AIR_UNITS.Text = item.Temp_units;
            ELEVATION.Text = item.Altitude.ToString();
            ELEVATION_UNITS.Text = item.Altitude_units;
            EXPECTED.Text = item.Expected_MAF.ToString();
            DIFF.Text = item.MAF_Difference.ToString();
            VOLUMETRIC.Text = item.Volumetric_Efficiency.ToString();
            CONDITION.Text = item.Condition;
            COMMENTS.Text = item.Comments;

            popUpPanelBackground.Visibility = Visibility.Visible;
            recordPopUp.Visibility = Visibility.Visible;
        }

        private void closeRecordPopUpButton_Click(object sender, RoutedEventArgs e)
        {
            popUpPanelBackground.Visibility = Visibility.Collapsed;
            recordPopUp.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Search

        private async void searchButton_Click(object sender, RoutedEventArgs e)
        {
            {
                noResults.Visibility = Visibility.Collapsed;
                mafChartNoResultsLabel.Visibility = Visibility.Collapsed;
                veChartNoResultsLabel.Visibility = Visibility.Collapsed;

                string YEAR = "";
                string MAKE = "";
                string MODEL = "";
                string ENGINE = "";
                string CONDITION = "";
                string COMMENTS = "";
                List<string> queryStringList = new List<string>();
                List<string> searchedForList = new List<string>();
                string queryString;

                try
                {
                    if (year.SelectedItem != null)
                    {
                        YEAR = year.SelectedItem.ToString();
                        searchedForList.Add(YEAR);

                        if (YEAR != "")
                        {
                            YEAR = " Year = '" + YEAR + "'";
                            queryStringList.Add(YEAR);
                        }
                    }

                    if (make.SelectedItem != null)
                    {
                        MAKE = make.SelectedItem.ToString();
                        searchedForList.Add(MAKE);

                        if (MAKE != "")
                        {
                            MAKE = " Make = '" + MAKE + "'";
                            queryStringList.Add(MAKE);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(model.Text))
                    {
                        MODEL = model.Text.Trim();
                        searchedForList.Add(MODEL);

                        if (MODEL.Length < 3)
                        {
                            MODEL = " Model = '" + MODEL + "'";
                            queryStringList.Add(MODEL);
                        }
                        else
                        {
                            MODEL = MODEL.Substring(0, 3);
                            MODEL = " Model LIKE '" + MODEL + "%'";
                            queryStringList.Add(MODEL);
                        }
                    }

                    if (engine.SelectedItem != null)
                    {
                        ENGINE = engine.SelectedItem.ToString();
                        searchedForList.Add(ENGINE);

                        if (ENGINE != "")
                        {
                            ENGINE = " Engine = '" + ENGINE + "'";
                            queryStringList.Add(ENGINE);
                        }
                    }

                    if (condition != null)
                    {
                        CONDITION = condition;
                        searchedForList.Add(CONDITION);

                        if (CONDITION != "")
                        {
                            CONDITION = " Condition = '" + CONDITION + "'";
                            queryStringList.Add(CONDITION);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(comments.Text))
                    {
                        COMMENTS = comments.Text.Trim();

                        // Split comments into individual words and remove separators like commas and white space
                        string[] separators = { ",", ".", "!", "?", ";", ":", " " };
                        string[] words = COMMENTS.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var word in words)
                        {
                            // Add each word as a keyword search of the Comments in the database
                            string KEYWORD = " Comments LIKE '%" + word + "%'";
                            queryStringList.Add(KEYWORD);

                            // Add each word to searchedForList
                            searchedForList.Add(word);
                        }
                    }

                    if (queryStringList.Count > 0)
                    {
                        ClearChartData();

                        // Set queryString based on items in queryStringList
                        if (queryStringList.Count == 1)
                        {
                            queryString = queryStringList.First().ToString();
                        }
                        else
                        {
                            queryString = string.Join(" AND", queryStringList);
                        }

                        // Query the database and update localRecords
                        var query = localDatabaseConnection.Query<MAFcalculation>("SELECT * FROM MAFcalculation WHERE" + queryString + " ORDER BY vehicleID DESC");
                        localRecords.ItemsSource = query;

                        // Set searchedForText
                        var searchText = string.Join(" ", searchedForList);
                        searchedForText.Text = searchText;
                        mafChartDataDescription.Text = searchText;
                        veChartDataDescription.Text = searchText;

                        // Manage noResults visibility, plot data if there are results
                        if (localRecords.Items.Count == 0)
                        {
                            noResults.Visibility = Visibility.Visible;
                            mafChartNoResultsLabel.Visibility = Visibility.Visible;
                            veChartNoResultsLabel.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            List<MAFcalculation> copyOfQuery = query.Select(item => new MAFcalculation(item)).ToList();
                            PlotDataOnCharts(copyOfQuery);
                        }

                        // Navigate to databasePivotItem if needed
                        if (databaseGrid.Children.Contains(recordsPanel))
                        {
                            mainPivot.SelectedItem = databasePivotItem;
                        }
                    }
                    else
                    {
                        var dialog = await new MessageDialog("Please select at least one option before searching.").ShowAsync();
                    }
                }
                catch
                {
                    var dialog = await new MessageDialog("A problem occurred when trying to search. Showing all records now.").ShowAsync();
                    ClearChartData();
                    ShowAllLocalRecords();
                }
            }
        }

        #endregion

        #region Plot data

        void PlotDataOnCharts(List<MAFcalculation> records)
        {
            if (records.Count < 1)
            {
                return;
            }
            else if (records.Count > 0)
            {
                records = AddCurrentCalculationToList(records);
                ConvertToGramsPerSecond(records);
                SetRpmScaleOnCharts(records);
                SetMafScaleOnChart(records);
                SetVeScaleOnChart(records);
                SetAmountsPerPixel();
                AddMafDataPlots(records);
                AddVeDataPlots(records);
            }
        }

        // Functions/Methods for data plotting //////////////////////////////////////////////

        void ClearChartData()
        {
            lowRPM = double.NaN;
            highRPM = double.NaN;
            lowMAF = double.NaN;
            highMAF = double.NaN;
            lowVE = double.NaN;
            highVE = double.NaN;
            rpmPerPixel = double.NaN;
            mafPerPixel = double.NaN;
            vePerPixel = double.NaN;

            highMafRpm.Text = "----";
            highVeRpm.Text = "----";
            lowMafRpm.Text = "----";
            lowVeRpm.Text = "----";
            lowMafFlow.Text = "----";
            highMafFlow.Text = "----";
            lowVePercent.Text = "----";
            highVePercent.Text = "----";

            vePlot.Children.Clear();
            mafPlot.Children.Clear();
        }

        List<MAFcalculation> ConvertToGramsPerSecond(List<MAFcalculation> records)
        {
            foreach (var record in records)
            {
                if (record.MAF_units == "kg/h")
                {
                    record.MAF = record.MAF / 3.6;
                    record.MAF_units = "g/s";
                }
            }

            return records;
        }

        List<MAFcalculation> ConvertToKilogramsPerHour(List<MAFcalculation> records)
        {
            foreach (var record in records)
            {
                if (record.MAF_units == "g/s")
                {
                    record.MAF = record.MAF * 3.6;
                    record.MAF_units = "kg/h";
                }
            }

            return records;
        }

        void SetRpmScaleOnCharts(List<MAFcalculation> records)
        {
            double minRecordRpm = records.Min(p => p.Engine_speed);
            double maxRecordRpm = records.Max(p => p.Engine_speed);

            if (minRecordRpm < 500)
            {
                lowRPM = 0;
            }
            else
            {
                lowRPM = minRecordRpm - 500;
            }
            highRPM = maxRecordRpm + 500;

            lowRPM = Math.Round(lowRPM);
            highRPM = Math.Round(highRPM);

            lowMafRpm.Text = lowRPM.ToString();
            lowVeRpm.Text = lowMafRpm.Text;
            highMafRpm.Text = highRPM.ToString();
            highVeRpm.Text = highMafRpm.Text;
        }

        void SetMafScaleOnChart(List<MAFcalculation> records)
        {
            double minRecordMaf = records.Min(p => p.MAF);
            double maxRecordMaf = records.Max(p => p.MAF);

            if (minRecordMaf < 25)
            {
                lowMAF = 0;
            }
            else
            {
                lowMAF = minRecordMaf - 25;
            }
            highMAF = maxRecordMaf + 25;

            lowMAF = Math.Round(lowMAF);
            highMAF = Math.Round(highMAF);

            lowMafFlow.Text = lowMAF.ToString();
            highMafFlow.Text = highMAF.ToString();
        }

        void SetVeScaleOnChart(List<MAFcalculation> records)
        {
            double minRecordVe = records.Min(p => p.Volumetric_Efficiency);
            double maxRecordVe = records.Max(p => p.Volumetric_Efficiency);

            if (minRecordVe < 10)
            {
                lowVE = 0;
            }
            else
            {
                lowVE = minRecordVe - 10;
            }
            highVE = maxRecordVe + 10;

            lowVE = Math.Round(lowVE);
            highVE = Math.Round(highVE);

            lowVePercent.Text = lowVE.ToString();
            highVePercent.Text = highVE.ToString();
        }

        void SetAmountsPerPixel()
        {
            rpmPerPixel = 380 / (highRPM - lowRPM);
            mafPerPixel = 380 / (highMAF - lowMAF);
            vePerPixel = 380 / (highVE - lowVE);
        }

        void AddMafDataPlots(List<MAFcalculation> records)
        {
            foreach (var record in records)
            {
                var leftMafMargin = (record.MAF - lowMAF) * mafPerPixel;
                var bottomRpmMargin = (record.Engine_speed - lowRPM) * rpmPerPixel;
                var plotColor = GetPlotColorBasedOnCondition(record);

                Ellipse ellipse = CreateEllipseForPlotChart(plotColor, leftMafMargin, bottomRpmMargin);

                var toolTip = new ToolTip();
                var recordMAF = Math.Round(record.MAF).ToString();
                var recordRPM = Math.Round(record.Engine_speed).ToString();
                toolTip.Content = recordMAF + " " + record.MAF_units + "\r" + recordRPM + "rpm";
                ToolTipService.SetToolTip(ellipse, toolTip);

                mafPlot.Children.Add(ellipse);
            }
        }

        void AddVeDataPlots(List<MAFcalculation> records)
        {
            foreach (var record in records)
            {
                var leftVeMargin = (record.Volumetric_Efficiency - lowVE) * vePerPixel;
                var bottomRpmMargin = (record.Engine_speed - lowRPM) * rpmPerPixel;
                var plotColor = GetPlotColorBasedOnCondition(record);

                Ellipse ellipse = CreateEllipseForPlotChart(plotColor, leftVeMargin, bottomRpmMargin);

                var toolTip = new ToolTip();
                var recordVE = Math.Round(record.Volumetric_Efficiency).ToString();
                var recordRPM = Math.Round(record.Engine_speed).ToString();
                toolTip.Content = recordVE + " %" + "\r" + recordRPM + " rpm";
                ToolTipService.SetToolTip(ellipse, toolTip);

                vePlot.Children.Add(ellipse);
            }
        }

        SolidColorBrush GetPlotColorBasedOnCondition(MAFcalculation record)
        {
            SolidColorBrush plotColor;

            switch (record.Condition)
            {
                case ("Good"):
                    plotColor = new SolidColorBrush(Colors.Lime);
                    break;
                case ("Bad"):
                    plotColor = new SolidColorBrush(Colors.Red);
                    break;
                case ("Unsure"):
                    plotColor = new SolidColorBrush(Colors.Yellow);
                    break;
                case ("CurrentCalculation"):
                    plotColor = new SolidColorBrush(Colors.White);
                    break;
                default:
                    plotColor = new SolidColorBrush(Colors.Gray);
                    break;
            }

            return plotColor;
        }

        Ellipse CreateEllipseForPlotChart(SolidColorBrush color, double left, double bottom)
        {
            Ellipse el = new Ellipse();
            el.Width = 10;
            el.Height = 10;
            el.Fill = color;
            el.Stroke = new SolidColorBrush(Colors.Black);
            el.StrokeThickness = 1;
            el.HorizontalAlignment = HorizontalAlignment.Left;
            el.VerticalAlignment = VerticalAlignment.Bottom;
            el.Margin = new Thickness(left, 0, 0, bottom);

            return el;
        }

        List<MAFcalculation> AddCurrentCalculationToList(List<MAFcalculation> records)
        {
            if (string.IsNullOrWhiteSpace(maf.Text) || string.IsNullOrWhiteSpace(VE.Text) || string.IsNullOrWhiteSpace(rpm.Text))
            {
                return records;
            }
            else
            {
                MAFcalculation c = new MAFcalculation();
                c.Condition = "CurrentCalculation";
                c.MAF_units = mafUnits.SelectedValue.ToString();
                c.Engine_speed = Convert.ToDouble(rpm.Text);
                c.MAF = Convert.ToDouble(maf.Text);
                c.Volumetric_Efficiency = Convert.ToDouble(VE.Text);

                records.Add(c);

                return records;
            }
        }


        #endregion

        #region Help

        private void instructionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (instructions.Visibility == Visibility.Collapsed)
            {
                instructions.Visibility = Visibility.Visible;
                expandInstructionsIcon.Glyph = "\uE971";
            }
            else
            {
                instructions.Visibility = Visibility.Collapsed;
                expandInstructionsIcon.Glyph = "\uE972";
            }
        }

        private void interpretResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (interpretResults.Visibility == Visibility.Collapsed)
            {
                interpretResults.Visibility = Visibility.Visible;
                expandInterpretResultsIcon.Glyph = "\uE971";
            }
            else
            {
                interpretResults.Visibility = Visibility.Collapsed;
                expandInterpretResultsIcon.Glyph = "\uE972";
            }
        }

        #endregion

        #region Chart viewability

        private void recordsViewPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = recordsViewPivot.SelectedItem;

            if (searchPanel.Height < 5) // Charts view is expanded
            {
                ShrinkChartsStoryboard.Begin();
            }
            else if (selectedItem == Charts) // Charts was selected and the Chart view has not been expanded
            {
                ExpandChartsStoryboard.Begin();
            }
        }

        #endregion

        #region Background options

        private void backgroundMenuItem_Click(object sender, RoutedEventArgs e)
        {
            popUpPanelBackground.Visibility = Visibility.Visible;
            backgroundOptionsPopUp.Visibility = Visibility.Visible;
        }

        private void backgroundOption_Checked(object sender, RoutedEventArgs e)
        {
            var radbtn = sender as RadioButton;
            var choice = radbtn.Name;
            if (localSettings != null)
            {
                if (choice == "yesImage")
                {
                    SetBackgroundImage();
                    localSettings.Values[BackgroundImageSetting] = true;
                }
                else
                {
                    backgroundImage.ClearValue(Image.SourceProperty);
                    copyright.ClearValue(TextBlock.TextProperty);
                    copyrightButton.BorderThickness = new Thickness(0);
                    localSettings.Values[BackgroundImageSetting] = false;
                }
            }
        }

        private void doneBackgroundOptions_Click(object sender, RoutedEventArgs e)
        {
            backgroundOptionsPopUp.Visibility = Visibility.Collapsed;
            popUpPanelBackground.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Other menu items

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            popUpPanelBackground.Visibility = Visibility.Visible;
            aboutPopUp.Visibility = Visibility.Visible;
            
            aboutTextBody.Text = "Version " + GetAppVersion() + "\n" +
                                 "Copyright \u00A9 2017 Steven McGrew \n" +
                                 "All rights reserved";
        }

        private void aboutCloseButton_Click(object sender, RoutedEventArgs e)
        {
            popUpPanelBackground.Visibility = Visibility.Collapsed;
            aboutPopUp.Visibility = Visibility.Collapsed;
        }

        string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            ushort[] versionProperties =
            {
                version.Major,
                version.Minor,
                version.Build,
                version.Revision
            };

            return String.Join(".", versionProperties);
        }

        #endregion

        
    }
}