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
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace MAF_VE_2
{
    public sealed partial class MainPage : Page
    {
        void Log(string str)
        {
            TextBlock t = new TextBlock();
            t.Text = str;
            backgroundImageLogPanel.Children.Add(t);
        }

        #region General variables

        WebAddresses webAddresses;
        SQLite.Net.SQLiteConnection localDatabaseConnection;
        List<string> allCarMakes;
        List<MAFcalculation> LocalCollection;
        List<MAFcalculation> GlobalCollection;
        bool rbCheckFired = false;
        string condition = "";
        const string ImageFileName = "BingImageOfTheDay.jpg";
        const string CopyrightFileName = "Copyright.txt";

        ApplicationDataContainer localSettings;
        const string BackgroundImageSetting = "BackgroundImageSetting";
        const string ShowBackupReminder = "ShowBackupReminder";
        const string DbBackupFileToken = "DbBackupFileToken";
        const string AutoBackupIsOn = "AutoBackupIsOn";
        const string localRecordCountAtLastBackup = "localRecordCountAtLastBackup";
        const string lastBackupTimeAndLocation = "lastBackupTimeAndLocation";

        #endregion

        #region Property for beginning data plotting

        private int _numberOfSearchesFinished;
        private int numberOfSearchesCompleted;
        public int NumberOfSearchesCompleted
        {
            get { return numberOfSearchesCompleted; }
            set
            {
                numberOfSearchesCompleted = value;
                if (numberOfSearchesCompleted == 2)
                {
                    PlotDataOnCharts(LocalCollection, GlobalCollection);
                }
            }
        }

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
            LocalCollection = new List<MAFcalculation>();
            GlobalCollection = new List<MAFcalculation>();
            webAddresses = new WebAddresses();

            // Initialize Settings storage
            localSettings = ApplicationData.Current.LocalSettings;
        }

        void InitializeLocalDatabase()
        {
            // Connect to local database and create tables if they don't exist
            string localDatabasePath = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "MAFdatabase.sqlite");
            localDatabaseConnection = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), localDatabasePath);
            localDatabaseConnection.CreateTable<MAFcalculation>();
            localDatabaseConnection.CreateTable<LocalCarMake>();
        }

        private void mainPage_Loaded(object sender, RoutedEventArgs e)
        {
            AddEngineComboboxItems();
            AddYearComboboxItems();
            RefreshMakesComboBox();
            ManageAutoBackupSetting();
            ShowAllLocalRecords();
            ShowRecentGlobalRecords();
            ManageBackgroundSetting();
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //// Register for PrintTaskRequested event
            //printMan = PrintManager.GetForCurrentView();
            //printMan.PrintTaskRequested += PrintTaskRequested;

            //// Build a PrintDocument and register for callbacks
            //printDoc = new PrintDocument();
            //printDocSource = printDoc.DocumentSource;
            //printDoc.Paginate += Paginate;
            //printDoc.GetPreviewPage += GetPreviewPage;
            //printDoc.AddPages += AddPages;

            //// Register the current page as a share source.
            //dataTransferManager = DataTransferManager.GetForCurrentView();
            //dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareImageHandler);

            // For shortcut keys
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //dataTransferManager.DataRequested -= ShareImageHandler;
            //printMan.PrintTaskRequested -= PrintTaskRequested;
            //printDoc.Paginate -= Paginate;
            //printDoc.GetPreviewPage -= GetPreviewPage;
            //printDoc.AddPages -= AddPages;
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
        }

        void ManageAutoBackupSetting()
        {
            Object autoBackupSetting = localSettings.Values[AutoBackupIsOn];
            bool IsStored = CheckIfSettingIsStored(autoBackupSetting);

            if (IsStored)
            {
                try
                {
                    bool backupIsOn = (bool)autoBackupSetting;
                    if (backupIsOn)
                    {
                        autoBackupToggle.IsOn = true;
                    }
                }
                catch (Exception ex)
                {
                    Log("ManageAutoBackupSetting error:  " + ex.Message);
                }
            }

            Object backupDateAndLocation = localSettings.Values[lastBackupTimeAndLocation];
            bool stored = CheckIfSettingIsStored(backupDateAndLocation);

            if (stored)
            {
                try
                {
                    string backupText = (string)backupDateAndLocation;
                    lastBackupText.Text = backupText;
                }
                catch (Exception ex)
                {
                    Log("SetBackupDateAndLocationText error:  " + ex.Message);
                }
            }
        }

        #endregion

        #region Bing image of the day

        async void ManageBackgroundSetting()
        {
            //Log("ManageBackgroundSetting");
            Object backgroundSetting = localSettings.Values[BackgroundImageSetting];
            bool IsStored = CheckIfSettingIsStored(backgroundSetting);
            if (IsStored)
            {
                //Log("Setting is stored...");
                bool IsShowImage = await CheckIfBackgroundSettingIsShowImage(backgroundSetting);
                if (IsShowImage)
                {
                    //Log("Setting is Show Image...");
                    yesImage.IsChecked = true;
                }
                else
                {
                    //Log("Setting is No Image...");
                    noImage.IsChecked = true;
                }
            }
            else
            {
                //Log("No setting is stored...");
                yesImage.IsChecked = true;
            }
        }

        async void ManageBackgroundImage()
        {
            //Log("ManageBackgroundImage");
            //Log("Try get image file...");
            IStorageItem imageFileItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync(ImageFileName);
            if (imageFileItem != null) // A file was previously saved and we were able to get it
            {
                //Log("Got image file...");
                bool imageWasSet = await SetBackgroundImage(imageFileItem);
                if (imageWasSet)
                {
                    //Log("Image was set...");
                    //Log("Try get copyright file...");
                    IStorageItem copyrightFileItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync(CopyrightFileName);
                    if (copyrightFileItem != null)
                    {
                        //Log("Got copyright file...");
                        SetCopyrightText(copyrightFileItem);
                    }
                    else
                    {
                        //Log("Problem getting copyright file...");
                    }

                    DownloadAndSetImageAndCopyrightIfNew();
                }
                else
                {
                    //Log("Problem setting image...");
                }
            }
            else // No file saved yet, or problem getting file
            {
                //Log("Did not get image file");
                SetDefaultBackgroundImage();
                DownloadAndSetImageAndCopyrightIfNew();
            }
        }

        bool CheckIfSettingIsStored(object setting)
        {
            //Log("CheckIfSettingIsStored");
            if (setting != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        async Task<bool> CheckIfBackgroundSettingIsShowImage(object setting)
        {
            //Log("CheckIfBackgroundSettingIsShowImage");

            try
            {
                bool settingIsShowImage = (bool)setting;
                if (settingIsShowImage)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Problem checking background image setting. \n \n" + ex.Message).ShowAsync();
                return false;
            }
        }

        async void SetImageAndCopyright()
        {
            //Log("SetImageAndCopyright");
            //Log("Try get image file...");
            IStorageItem imageFileItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync(ImageFileName);
            if (imageFileItem != null)
            {
                //Log("Got image file...");
                bool imageWasSet = await SetBackgroundImage(imageFileItem);
                if (imageWasSet)
                {
                    //Log("Image was set...");
                    //Log("Try get copyright file...");
                    IStorageItem copyrightFileItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync(CopyrightFileName);
                    if (copyrightFileItem != null)
                    {
                        //Log("Got copyright file...");
                        SetCopyrightText(copyrightFileItem);
                    }
                    else
                    {
                        //Log("Problem getting copyright file...");
                    }
                }
                else
                {
                    //Log("Problem setting image...");
                }
            }
            else
            {
                //Log("Did not get image file...");
            }
        }

        async Task<bool> SetBackgroundImage(IStorageItem storageItem)
        {
            //Log("SetBackgroundImage");
            try
            {
                StorageFile file = storageItem as StorageFile;
                using (var stream = await file.OpenReadAsync())
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);
                    backgroundImage.Source = bitmapImage;
                }
                return true;
            }
            catch (Exception ex)
            {
                //Log("Deleting potentially corrupt image file...");
                var potentiallyCorruptFile = await ApplicationData.Current.LocalFolder.GetFileAsync(ImageFileName);
                await potentiallyCorruptFile.DeleteAsync();

                await new MessageDialog("Problem setting background image. \n \n" + ex.Message).ShowAsync();
                backgroundImage.Source = new BitmapImage(new Uri(BaseUri, "/Assets/hdBackground.png"));
                return false;
            }
        }

        async void SetCopyrightText(IStorageItem storageItem)
        {
            //Log("SetCopyrightText");
            try
            {
                StorageFile file = storageItem as StorageFile;
                BasicProperties props = await file.GetBasicPropertiesAsync();
                string copyrightText = await FileIO.ReadTextAsync(file);

                copyright.Text = "Image: " + copyrightText;
                copyrightButton.BorderThickness = new Thickness(1);

                DateTimeOffset dateOfLastMod = props.DateModified;
                string dateTime = dateOfLastMod.DateTime.ToString();
                //Log("File last modified: " + dateTime);
            }
            catch (Exception ex)
            {
                //Log("Deleting potentially corrupt copyright file...");
                var potentiallyCorruptFile = await ApplicationData.Current.LocalFolder.GetFileAsync(CopyrightFileName);
                await potentiallyCorruptFile.DeleteAsync();

                await new MessageDialog("Problem setting copyright text. \n \n" + ex.Message).ShowAsync();
                copyright.Text = "Image: Could not get copyright info";
                copyrightButton.BorderThickness = new Thickness(1);
            }
        }

        async Task<JsonObject> CheckForNewImage()
        {
            //Log("CheckForNewImage");
            JsonObject jsonObject;
            string JSON = await GetBingImageJSON();

            if (JSON != null)
            {
                //Log("Got JSON...");
                jsonObject = await ParseJSON(JSON);

                if (jsonObject != null)
                {
                    try
                    {
                        string copyrightText = jsonObject["images"].GetArray()[0].GetObject()["copyright"].GetString();
                        StorageFile savedCopyrightFile = await ApplicationData.Current.LocalFolder.GetFileAsync(CopyrightFileName);
                        string savedCopyrightText = await FileIO.ReadTextAsync(savedCopyrightFile);

                        if (copyrightText == savedCopyrightText)
                        {
                            //Log("No new image available yet...");
                            return jsonObject = null; // New image not available yet
                        }
                        else
                        {
                            //Log("New image is available...");
                            return jsonObject; // New image is available
                        }
                    }
                    catch
                    {
                        //Log("Problem comparing copyright text, continuing on...");
                        return jsonObject; // The problem was likely that there was no copyright file, so we want to continue on to download the image and copyright
                    }
                }
                else
                {
                    return jsonObject = null; // Problem parsing JSON
                }
            }
            else
            {
                //Log("Did not get JSON, check internet...");
                return jsonObject = null; // Problem getting JSON
            }
        }

        async Task<string> GetBingImageJSON()
        {
            //Log("GetBingImageJSON");
            string region = "en-US";
            int numberOfImages = 1;
            string bingImageURL = string.Format("http://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n={0}&mkt={1}", numberOfImages, region);
            string JSON;

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage httpResponse = await httpClient.GetAsync(new Uri(bingImageURL));
                    return JSON = await httpResponse.Content.ReadAsStringAsync();
                }
            }
            catch
            {
                return JSON = null;
            }
        }

        async Task<JsonObject> ParseJSON(string JSON)
        {
            //Log("ParseJSON");
            JsonObject jsonObject;
            try
            {
                bool IsParsed = JsonObject.TryParse(JSON, out jsonObject);
                if (IsParsed)
                {
                    //Log("JSON was parsed...");
                    return jsonObject;
                }
                else
                {
                    //Log("JSON was not parsed...");
                    return jsonObject = null;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Problem trying to parse JSON. \n \n" + ex.Message).ShowAsync();
                return jsonObject = null;
            }
        }

        async Task<bool> DownloadBingImageToFile(JsonObject jsonObject)
        {
            //Log("DownloadBingImageToFile");
            try
            {
                string partialUrlForImage = jsonObject["images"].GetArray()[0].GetObject()["url"].GetString();
                string completeUrlForImage = "https://www.bing.com" + partialUrlForImage;
                Uri bingUri = new Uri(completeUrlForImage);
                string fileName = ImageFileName;

                RandomAccessStreamReference IRASRstream = RandomAccessStreamReference.CreateFromUri(bingUri);
                StorageFile remoteFile = await StorageFile.CreateStreamedFileFromUriAsync(fileName, bingUri, IRASRstream);
                //Log("Downloading...");
                await remoteFile.CopyAsync(ApplicationData.Current.LocalFolder, fileName, NameCollisionOption.ReplaceExisting);

                //Log("Successfully downloaded to file...");
                return true;
            }
            catch (Exception ex)
            {
                await new MessageDialog("Problem downloading image. A slow internet connection can cause this problem. \n \n" + ex.Message).ShowAsync();

                //Log("Failed to download to file, check internet...");
                return false;
            }
        }

        async Task<bool> SaveCopyrightToFile(JsonObject jsonObject)
        {
            //Log("SaveCopyrightToFile");
            try
            {
                string copyrightText = jsonObject["images"].GetArray()[0].GetObject()["copyright"].GetString();
                StorageFile copyrightFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(CopyrightFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(copyrightFile, copyrightText);

                //Log("Successfully saved copyright text...");
                return true;
            }
            catch (Exception ex)
            {
                //Log("Failed to save copyright text...");
                await new MessageDialog("Problem saving copyright text. \n \n" + ex.Message).ShowAsync();
                return false;
            }
        }

        void SetDefaultBackgroundImage()
        {
            //Log("SetDefaultBackgroundImage");
            backgroundImage.Source = new BitmapImage(new Uri(BaseUri, "/Assets/hdBackground.png"));
        }

        async void DownloadAndSetImageAndCopyrightIfNew()
        {
            //Log("DownloadAndSetImageAndCopyrightIfNew");

            JsonObject newImageJsonObject = await CheckForNewImage();

            if (newImageJsonObject != null) // New image is available
            {
                bool imageDownloaded = await DownloadBingImageToFile(newImageJsonObject);
                bool copyrightSaved = await SaveCopyrightToFile(newImageJsonObject);

                if (imageDownloaded)
                {
                    SetImageAndCopyright();
                }
            }
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
                    //Log("  yesImage Checked");
                    ManageBackgroundImage();
                    localSettings.Values[BackgroundImageSetting] = true;
                }
                else
                {
                    //Log("  noImage Checked");
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

                // Replace spaces with dashes.
                fileName = fileName.Replace(' ', '-');
            }
            catch
            {
                fileName = "Bing_image_of_the_day";
            }

            return fileName;
        }

        #endregion

        #region Databases

        void BeginWaitForDb()
        {
            resetButton.IsEnabled = false;
            saveButton.IsEnabled = false;
            searchButton.IsEnabled = false;
            progressLocal.Visibility = Visibility.Visible;
        }

        void EndWaitForDb()
        {
            progressLocal.Visibility = Visibility.Collapsed;
            resetButton.IsEnabled = true;
            saveButton.IsEnabled = true;
            searchButton.IsEnabled = true;
        }

        Task<List<MAFcalculation>> QueryLocalDatabase(string queryString)
        {
            return Task.Run(() =>
            {
                return localDatabaseConnection.Query<MAFcalculation>("SELECT * FROM MAFcalculation WHERE" + queryString + " ORDER BY vehicleID DESC LIMIT 1000");
            });
        }

        Task<List<MAFcalculation>> GetAllLocalRecordsAsync()
        {
            return Task.Run(() =>
             {
                 return localDatabaseConnection.Query<MAFcalculation>("SELECT * from MAFcalculation ORDER BY vehicleID DESC");
             });
        }

        Task<List<MAFcalculation>> GetRecentGlobalRecordsAsync()
        {
            return Task.Run(async () =>
            {
                var records = new List<MAFcalculation>();

                // Get JSON
                string JSON;
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.IfModifiedSince = DateTime.Now; // This property must be set to get most current data. Otherwise, cached data will be returned.
                    HttpResponseMessage httpResponse = await httpClient.GetAsync(new Uri(webAddresses.Last100Url));
                    JSON = await httpResponse.Content.ReadAsStringAsync();
                }

                // Parse JSON
                JsonArray jsonArray;
                bool IsParsed = JsonArray.TryParse(JSON, out jsonArray);
                if (IsParsed)
                {
                    // Build collection and return it
                    foreach (var item in jsonArray)
                    {
                        var jsonString = item.ToString();
                        records.Add(new MAFcalculation(jsonString));
                    }
                    return records;
                }
                else
                {
                    return records;
                }
            });
        }

        async void ShowAllLocalRecords()
        {
            //Log("ShowAllLocalRecords");
            BeginWaitForDb();

            //Log("GetAllLocalRecordsAsync");
            LocalCollection = await GetAllLocalRecordsAsync();
            LoadLocalRecords(LocalCollection, localRecords);

            if (LocalCollection.Count == 0)
            {
                noResults.Visibility = Visibility.Visible;
            }
            else
            {
                noResults.Visibility = Visibility.Collapsed;
            }

            searchedForText.Text = "all records";
            searchedForPanelStory.Begin();
            veChartDataDescription.Text = "Perform a search to see data in this chart";
            mafChartDataDescription.Text = "Perform a search to see data in this chart";
            ClearChartData();

            EndWaitForDb();
        }

        async void ShowRecentGlobalRecords()
        {
            progressGlobal.Visibility = Visibility.Visible;
            GlobalCollection.Clear();

            var success = false;
            try
            {
                GlobalCollection = await GetRecentGlobalRecordsAsync();
                success = true;
            }
            catch
            {
                success = false;
            }
            

            if (success == false)
            {
                noResultsGlobal.Text = "Global database not available.\r\nCheck internet connection or try again later.";
                noResultsGlobal.Visibility = Visibility.Visible;
            }
            else
            {
                noResultsGlobal.Text = "No results found.";

                if (GlobalCollection.Count == 0)
                {
                    noResultsGlobal.Visibility = Visibility.Visible;
                }
                else
                {

                    noResultsGlobal.Visibility = Visibility.Collapsed;
                }

                LoadGlobalRecords(GlobalCollection, globalRecords);
            }

            searchedForTextGlobal.Text = "most recent 100 records";
            searchedForPanelStoryGlobal.Begin();
            progressGlobal.Visibility = Visibility.Collapsed;
        }

        void LoadLocalRecords(List<MAFcalculation> records, ListView listView)
        {
            localScrollViewer.ViewChanged -= localScrollViewer_ViewChanged;
            localScrollViewer.ChangeView(0.0, 0.0, null, true);

            listView.Items.Clear();

            var viewHeight = localScrollViewer.ViewportHeight;
            var amountToLoad = Convert.ToInt32((viewHeight * 2) / 44);
            if (amountToLoad < 20)
            {
                amountToLoad = 20;
            }

            if (records.Count > amountToLoad)
            {
                // Add only the amountToLoad to start with
                Log("LoadRecords, " + amountToLoad.ToString());
                for (int i = 0; i < amountToLoad; i++)
                {
                    var itemToBeAdded = records.ElementAt(i);
                    listView.Items.Add(itemToBeAdded);
                }
            }
            else
            {
                // Add all records since there aren't a lot
                Log("LoadRecords, all");
                for (int i = 0; i < records.Count; i++)
                {
                    var itemToBeAdded = records.ElementAt(i);
                    listView.Items.Add(itemToBeAdded);
                }
            }

            localScrollViewer.ViewChanged += localScrollViewer_ViewChanged;
        }

        void LoadGlobalRecords(List<MAFcalculation> records, ListView listView)
        {
            globalScrollViewer.ChangeView(0.0, 0.0, null, true);
            listView.Items.Clear();

            for (int i = 0; i < records.Count; i++)
            {
                var itemToBeAdded = records.ElementAt(i);
                listView.Items.Add(itemToBeAdded);
            }
        }

        private void localScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scrollviewer = sender as ScrollViewer;

            if (scrollviewer.VerticalOffset >= scrollviewer.ScrollableHeight) // scrollviewer is at bottom
            {
                var listviewCount = localRecords.Items.Count();
                var collectionCount = LocalCollection.Count();
                var numberOfRecordsRemaining = collectionCount - listviewCount;

                if (numberOfRecordsRemaining <= 0) // No more records to add to listview
                {

                }
                else
                {
                    Log("scroll add records");
                    var indexToStartAt = collectionCount - numberOfRecordsRemaining;
                    var indexToEndAt = LocalCollection.Count - 1;
                    if (numberOfRecordsRemaining > 20)
                    {
                        indexToEndAt = indexToStartAt + 19;
                    }

                    for (int i = indexToStartAt; i <= indexToEndAt; i++)
                    {
                        var itemToBeAdded = LocalCollection.ElementAt(i);
                        localRecords.Items.Add(itemToBeAdded);
                    }
                }
            }
            else // scrollviewer not at bottom
            {

            }
        }

        #endregion

        #region Backup options

        void BeginLocalDbBackupOption()
        {
            Log("BeginLocalDbBackupOption");
            var currentCount = localDatabaseConnection.Table<MAFcalculation>().Count();

            var backupIsOn = autoBackupToggle.IsOn;
            if (backupIsOn)
            {
                Object savedCount = localSettings.Values[localRecordCountAtLastBackup];

                bool stored = CheckIfSettingIsStored(savedCount);
                if (stored) // Backup should have been stored before, and savedCount is available
                {
                    try
                    {
                        int countAtLastBackup = (int)savedCount;

                        if (currentCount - countAtLastBackup > 2)
                        {
                            AutosaveLocalDbBackup();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("BeginLocalDbBackupOption error1:  " + ex.Message);
                    }
                }
                else // Backup has not been stored before (This section of code is reached when auto backup toggle switch was toggled to on and no backup has been saved before)
                {
                    popUpPanelBackground.Visibility = Visibility.Visible;
                    localDbBackupPopUp.Visibility = Visibility.Visible;
                    popUpForAutoBackupToggle.Visibility = Visibility.Visible;
                }
            }
            else
            {
                Object backupReminderSetting = localSettings.Values[ShowBackupReminder];
                bool IsStored = CheckIfSettingIsStored(backupReminderSetting);
                if (IsStored)
                {
                    try
                    {
                        bool showReminder = (bool)backupReminderSetting;
                        if (showReminder)
                        {
                            ShowBackupReminderEvery3rdRecord(currentCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("BeginLocalDbBackupOption error2:  " + ex.Message);
                    }
                }
                else
                {
                    ShowBackupReminderEvery3rdRecord(currentCount);
                }
            }
        }

        void ShowBackupReminderEvery3rdRecord(int recordCount)
        {
            Log("ShowBackupReminderEvery3rdRecord");
            if (recordCount == 0)
            {
                // Do nothing. No records saved yet.
            }
            else
            {
                if (recordCount % 3 == 0) // if currentCount is divisible by three, then remainder would be zero, so show reminder
                {
                    ShowBackupReminderPopup();
                }
            }
        }

        private void yesBackupButton_Click(object sender, RoutedEventArgs e)
        {
            SaveLocalDatabaseBackup();
            localSettings.Values[ShowBackupReminder] = false;
            askToBackupPopUp.Visibility = Visibility.Collapsed;
            popUpPanelBackground.Visibility = Visibility.Collapsed;
        }

        private void noBackupButton_Click(object sender, RoutedEventArgs e)
        {
            askToBackupPopUp.Visibility = Visibility.Collapsed;
            popUpPanelBackground.Visibility = Visibility.Collapsed;
        }

        private void dontAskBackupButton_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values[ShowBackupReminder] = false;

            askToBackupPopUp.Visibility = Visibility.Collapsed;
            popUpPanelBackground.Visibility = Visibility.Collapsed;
        }

        private void autoBackupToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;

            if (toggleSwitch.IsOn)
            {
                BeginLocalDbBackupOption();
                localSettings.Values[AutoBackupIsOn] = true;
            }
            else
            {
                localSettings.Values[AutoBackupIsOn] = false;
            }
        }

        private void backupNowButton_Click(object sender, RoutedEventArgs e)
        {
            AutosaveLocalDbBackup();
            if (lastBackupText.Text.Count() > 24)
            {
                savedPopupStory.Begin();
            }
        }

        private void changeLocationButton_Click(object sender, RoutedEventArgs e)
        {
            SaveLocalDatabaseBackup();
        }

        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            localDbBackupPopUp.Visibility = Visibility.Collapsed;

            generalPopup.Visibility = Visibility.Visible;
            generalText.Text = "Import :  In the next step, you will need to chose a SQLITE (.sqlite) file that was previously created as a backup of this app.\r\nOther SQLITE files that were not created for this app will not work.";
        }

        private void importAndMergeButton_Click(object sender, RoutedEventArgs e)
        {
            localDbBackupPopUp.Visibility = Visibility.Collapsed;

            generalPopup.Visibility = Visibility.Visible;
            generalText.Text = "Merge :  In the next step, you will need to chose a SQLITE (.sqlite) file that was previously created as a backup of this app.\r\nOther SQLITE files that were not created for this app will not work.";
        }

        private void dataBackupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            popUpPanelBackground.Visibility = Visibility.Visible;
            localDbBackupPopUp.Visibility = Visibility.Visible;
        }

        private void closeBackupPopupButton_Click(object sender, RoutedEventArgs e)
        {
            localDbBackupPopUp.Visibility = Visibility.Collapsed;
            popUpPanelBackground.Visibility = Visibility.Collapsed;
        }

        private void generalOkButton_Click(object sender, RoutedEventArgs e)
        {
            generalPopup.Visibility = Visibility.Collapsed;

            if (generalText.Text.StartsWith("I")) // starts with "Import"
            {
                ImportSQLiteFile();
            }
            else if (generalText.Text.StartsWith("M")) // starts with "Merge"
            {
                ImportAndMergeSQLiteFile();
            }

            popUpPanelBackground.Visibility = Visibility.Collapsed;
            generalText.ClearValue(TextBlock.TextProperty);
            gridForMergeNotes.Visibility = Visibility.Collapsed;
        }

        private void popUpForAutoBackupToggle_OKbtn_Click(object sender, RoutedEventArgs e)
        {
            SaveLocalDatabaseBackup();
            popUpForAutoBackupToggle.Visibility = Visibility.Collapsed;
        }

        // Functions for Data Backup /////////////////////////////////////////////////////////////

        void ShowBackupReminderPopup()
        {
            Log("ShowBackupReminderPopup");
            localSettings.Values[ShowBackupReminder] = true;

            popUpPanelBackground.Visibility = Visibility.Visible;
            askToBackupPopUp.Visibility = Visibility.Visible;
        }

        async void AutosaveLocalDbBackup()
        {
            Log("AutosaveLocalDbBackup");
            Object token = localSettings.Values[DbBackupFileToken];
            bool settingIsStored = CheckIfSettingIsStored(token);

            if (settingIsStored)
            {
                try
                {
                    dbButtonsPanel.IsHitTestVisible = false;
                    //localDatabaseConnection.Close();

                    string fileAccessToken = (string)token;
                    var storageFile = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(fileAccessToken);

                    var dbFile = await ApplicationData.Current.LocalFolder.GetFileAsync("MAFdatabase.sqlite");
                    if (dbFile != null)
                    {
                        await dbFile.CopyAndReplaceAsync(storageFile);

                        var now = System.DateTime.Now;
                        lastBackupText.Text = "Last Backup:   " + now + Environment.NewLine +
                                              "Location:   " + storageFile.Path;

                        localSettings.Values[lastBackupTimeAndLocation] = lastBackupText.Text;
                    }
                    else
                    {
                        var dialog = await new MessageDialog("A problem occured when trying to auto-backup app data.").ShowAsync();
                    }
                }
                catch
                {
                    var dialog = await new MessageDialog("A problem occured when trying to auto-backup app data.").ShowAsync();
                }
                finally
                {
                    //InitializeLocalDatabase();
                    localSettings.Values[localRecordCountAtLastBackup] = localDatabaseConnection.Table<MAFcalculation>().Count();
                    dbButtonsPanel.IsHitTestVisible = true;
                }
            }
            else
            {
                SaveLocalDatabaseBackup();
            }
        }

        async void SaveLocalDatabaseBackup()
        {
            Log("SaveLocalDatabaseBackup");
            var localRecordsCount = localDatabaseConnection.Table<MAFcalculation>().Count();

            bool success = false;
            try
            {
                dbButtonsPanel.IsHitTestVisible = false;
                //localDatabaseConnection.Close();

                var dbFile = await ApplicationData.Current.LocalFolder.GetFileAsync("MAFdatabase.sqlite");
                if (dbFile != null)
                {
                    var savePicker = new FileSavePicker();
                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    savePicker.FileTypeChoices.Add("SQLite database", new List<string>() { ".sqlite" });
                    savePicker.SuggestedFileName = "MAF-VE-backup.sqlite";

                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        await dbFile.CopyAndReplaceAsync(file);

                        string token = StorageApplicationPermissions.FutureAccessList.Add(file);
                        localSettings.Values[DbBackupFileToken] = token;

                        success = true;

                        var now = System.DateTime.Now;
                        lastBackupText.Text = "Last Backup:   " + now + Environment.NewLine +
                                              "Location:   " + file.Path;

                        localSettings.Values[lastBackupTimeAndLocation] = lastBackupText.Text;

                        savedPopupStory.Begin();
                    }
                    else // operation cancelled
                    {
                        if (lastBackupText.Text.Count() < 25) // no backups have ever been saved
                        {
                            autoBackupToggle.IsOn = false;
                        }
                    }
                }
                else
                {
                    var dialog = await new MessageDialog("A problem occured when trying to get the file.").ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = await new MessageDialog("A problem occured when trying to save database file. " + ex.Message).ShowAsync();
            }
            finally
            {
                //InitializeLocalDatabase();

                if (success)
                {
                    localSettings.Values[localRecordCountAtLastBackup] = localRecordsCount;
                    autoBackupToggle.IsOn = true;
                }

                dbButtonsPanel.IsHitTestVisible = true;
            }
        }

        async void ImportSQLiteFile()
        {
            Log("ImportSQLiteFile");
            FileOpenPicker picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".sqlite");
            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                if (file.FileType == ".sqlite")
                {
                    bool success = false;
                    try
                    {
                        popUpPanelBackground.Visibility = Visibility.Visible;
                        generalOkButton.Visibility = Visibility.Collapsed;
                        generalText.Text = " Importing file...";
                        generalPopup.Visibility = Visibility.Visible;
                        generalProgressRing.Visibility = Visibility.Visible;
                        generalProgressRing.IsActive = true;
                        dbButtonsPanel.IsHitTestVisible = false;

                        localDatabaseConnection.Close();

                        var localAppFile = await ApplicationData.Current.LocalFolder.GetFileAsync("MAFdatabase.sqlite");
                        await file.CopyAndReplaceAsync(localAppFile);

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        var dialog = await new MessageDialog("A problem occured when trying to import the file." + ex.Message).ShowAsync();
                    }
                    finally
                    {
                        InitializeLocalDatabase();
                        RefreshMakesComboBox();
                        ShowAllLocalRecords();

                        generalProgressRing.IsActive = false;
                        generalProgressRing.Visibility = Visibility.Collapsed;
                        generalOkButton.Visibility = Visibility.Visible;

                        if (success)
                        {
                            var count = localDatabaseConnection.Table<MAFcalculation>().Count();
                            //("SELECT Count(*) FROM MAFcalculation")

                            popUpPanelBackground.Visibility = Visibility.Visible;
                            generalPopup.Visibility = Visibility.Visible;
                            generalText.Text = "The import was successful." + Environment.NewLine +
                                                Environment.NewLine +
                                               "All previous data was removed, and the chosen file was imported." + Environment.NewLine +
                                               "There were " + count.ToString() + " vehicle records in the file.";
                        }

                        dbButtonsPanel.IsHitTestVisible = true;
                    }
                }
                else
                {
                    var dialog = await new MessageDialog("Wrong file type detected. Make sure to choose a SQLITE (.sqlite) file.").ShowAsync();
                }
            }
            else // FileOpenPicker canceled
            {
                popUpPanelBackground.Visibility = Visibility.Collapsed;
            }
        }

        async void ImportAndMergeSQLiteFile()
        {
            Log("ImportAndMergeSQLiteFile");
            FileOpenPicker picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".sqlite");
            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                if (file.FileType == ".sqlite")
                {
                    var countBeforeMerge = localDatabaseConnection.Table<MAFcalculation>().Count();
                    var numberOfRecordsInImportFile = 0;
                    int numberOfSuccefulInserts = 0;
                    int numberOfFailedInserts = 0;
                    int numberOfDuplicatesDetected = 0;
                    bool success = false;
                    try
                    {
                        popUpPanelBackground.Visibility = Visibility.Visible;
                        generalOkButton.Visibility = Visibility.Collapsed;
                        generalText.Text = " Merging records...";
                        generalPopup.Visibility = Visibility.Visible;
                        generalProgressRing.Visibility = Visibility.Visible;
                        generalProgressRing.IsActive = true;
                        dbButtonsPanel.IsHitTestVisible = false;

                        var importedFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("ImportedFile.sqlite", CreationCollisionOption.ReplaceExisting);
                        await file.CopyAndReplaceAsync(importedFile);

                        string dbPath = System.IO.Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "ImportedFile.sqlite");
                        SQLite.Net.SQLiteConnection dbConn = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), dbPath);

                        numberOfRecordsInImportFile = dbConn.Table<MAFcalculation>().Count();
                        var records = dbConn.Query<MAFcalculation>("SELECT * from MAFcalculation");
                        foreach (var record in records)
                        {
                            List<string> queryStringList = new List<string>();
                            string YEAR = " Year = '" + record.Year + "'";
                            string MAKE = " Make = '" + record.Make + "'";
                            string MODEL = " Model = '" + record.Model + "'";
                            string ENGINE = " Engine = '" + record.Engine + "'";
                            string CONDITION = " Condition = '" + record.Condition + "'";
                            string MAF = " MAF = '" + Convert.ToString(record.MAF) + "'";
                            string VE = " Volumetric_Efficiency = '" + Convert.ToString(record.Volumetric_Efficiency) + "'";
                            queryStringList.Add(YEAR);
                            queryStringList.Add(MAKE);
                            queryStringList.Add(MODEL);
                            queryStringList.Add(ENGINE);
                            queryStringList.Add(CONDITION);
                            queryStringList.Add(MAF);
                            queryStringList.Add(VE);

                            string queryString = string.Join(" AND", queryStringList);
                            var query = localDatabaseConnection.Query<MAFcalculation>("SELECT * FROM MAFcalculation WHERE" + queryString + " LIMIT 1");

                            if (query.Count == 0)
                            {
                                try
                                {
                                    localDatabaseConnection.Insert(new MAFcalculation()
                                    {
                                        Year = record.Year,
                                        Make = record.Make,
                                        Model = record.Model,
                                        Engine = record.Engine,
                                        Condition = record.Condition,
                                        Comments = record.Comments,
                                        Engine_speed = record.Engine_speed,
                                        MAF = record.MAF,
                                        Engine_size = record.Engine_size,
                                        Air_temperature = record.Air_temperature,
                                        Altitude = record.Altitude,
                                        Expected_MAF = record.Expected_MAF,
                                        MAF_Difference = record.MAF_Difference,
                                        Volumetric_Efficiency = record.Volumetric_Efficiency,
                                        MAF_units = record.MAF_units,
                                        Temp_units = record.Temp_units,
                                        Altitude_units = record.Altitude_units
                                    });

                                    numberOfSuccefulInserts++;
                                }
                                catch
                                {
                                    numberOfFailedInserts++;
                                }
                            }
                            else
                            {
                                numberOfDuplicatesDetected++;
                            }
                        }

                        var makes = dbConn.Query<LocalCarMake>("SELECT * from LocalCarMake");
                        foreach (var make in makes)
                        {
                            var queryMakes = localDatabaseConnection.Query<LocalCarMake>("SELECT * FROM LocalCarMake WHERE Make = '" + make.Make + "' LIMIT 1");
                            if (queryMakes.Count == 0)
                            {
                                localDatabaseConnection.Insert(new LocalCarMake()
                                {
                                    Make = make.Make
                                });
                            }
                        }

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        var dialog = await new MessageDialog("A problem occured when trying to import and merge the file.  " + ex.Message).ShowAsync();
                    }
                    finally
                    {
                        InitializeLocalDatabase();
                        RefreshMakesComboBox();
                        ShowAllLocalRecords();

                        generalProgressRing.IsActive = false;
                        generalProgressRing.Visibility = Visibility.Collapsed;
                        generalOkButton.Visibility = Visibility.Visible;

                        if (success)
                        {
                            var countAfterMerge = localDatabaseConnection.Table<MAFcalculation>().Count();

                            popUpPanelBackground.Visibility = Visibility.Visible;
                            generalPopup.Visibility = Visibility.Visible;
                            generalText.Text = "The merge operation completed.";
                            gridForMergeNotes.Visibility = Visibility.Visible;
                            beforeTblk.Text = countBeforeMerge.ToString();
                            successTblk.Text = numberOfSuccefulInserts.ToString();
                            totalTblk.Text = countAfterMerge.ToString();
                            duplicatesTblk.Text = numberOfDuplicatesDetected.ToString();
                            failedTblk.Text = numberOfFailedInserts.ToString();
                        }

                        dbButtonsPanel.IsHitTestVisible = true;
                    }
                }
                else
                {
                    var dialog = await new MessageDialog("Wrong file type detected. Make sure to choose a SQLITE (.sqlite) file.").ShowAsync();
                }
            }
            else // FileOpenPicker canceled
            {
                popUpPanelBackground.Visibility = Visibility.Collapsed;
            }
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
                        Make = makeToAdd
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

            AddRecordsWhenViewGrows(localScrollViewer.ViewportHeight);
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

        void AddRecordsWhenViewGrows(double newViewHeight)
        {
            if (LocalCollection != null)
            {
                if (localRecords.ActualHeight < newViewHeight)
                {
                    var newHeightWeWant = newViewHeight * 2;
                    var amountToLoad = Convert.ToInt32((newHeightWeWant - localRecords.ActualHeight) / 44);
                    var listviewCount = localRecords.Items.Count();
                    var collectionCount = LocalCollection.Count();
                    var numberOfRecordsRemaining = collectionCount - listviewCount;

                    if (numberOfRecordsRemaining <= 0) // No more records to add to listview
                    {

                    }
                    else
                    {
                        int indexToStartAt = collectionCount - numberOfRecordsRemaining;
                        int indexToEndAt;
                        if (numberOfRecordsRemaining < amountToLoad)
                        {
                            indexToEndAt = LocalCollection.Count - 1; // Load all remaining records
                        }
                        else
                        {
                            indexToEndAt = indexToStartAt + (amountToLoad - 1); // Only load amountToLoad (the -1 is so we choose the correct index)
                        }

                        for (int i = indexToStartAt; i <= indexToEndAt; i++)
                        {
                            var itemToBeAdded = LocalCollection.ElementAt(i);
                            localRecords.Items.Add(itemToBeAdded);
                        }
                    }
                }
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
                    recordsViewPivot.SelectedItem = Local;
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
            Reset();
        }

        void Reset()
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
            ShowRecentGlobalRecords();
        }

        #endregion

        #region Save

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        async void Save()
        {
            var dialog1 = new MessageDialog("Must enter Year, Make, Model, Engine, and select Good/Bad/Unsure. Only the comments section is optional.");
            bool success = false;

            MAFcalculation recordToSave = new MAFcalculation();
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
                recordToSave.Year = _year;
                recordToSave.Make = _make;
                recordToSave.Model = _model;
                recordToSave.Engine = _engine;
                recordToSave.Condition = _condition;
                recordToSave.Comments = _comments;
                recordToSave.Engine_speed = Convert.ToDouble(rpm.Text);
                recordToSave.MAF = Convert.ToDouble(maf.Text);
                recordToSave.Engine_size = Convert.ToDouble(engineSize.Text);
                recordToSave.Air_temperature = Convert.ToDouble(airTemp.Text);
                recordToSave.Altitude = Convert.ToDouble(altitude.Text);
                recordToSave.Expected_MAF = Convert.ToDouble(expectedMAF.Text);
                recordToSave.MAF_Difference = Convert.ToDouble(mafDifference.Text);
                recordToSave.Volumetric_Efficiency = Convert.ToDouble(VE.Text);
                recordToSave.MAF_units = mafUnits.SelectedValue.ToString();
                recordToSave.Temp_units = airTempUnits.SelectedValue.ToString();
                recordToSave.Altitude_units = altitudeUnits.SelectedValue.ToString();

                localDatabaseConnection.Insert(recordToSave);

                good.ClearValue(RadioButton.IsCheckedProperty);
                bad.ClearValue(RadioButton.IsCheckedProperty);
                unsure.ClearValue(RadioButton.IsCheckedProperty);
                condition = "";
                comments.ClearValue(TextBox.TextProperty);

                success = true;
            }
            catch
            {
                await new MessageDialog("Failed to save. Make sure your inputs in the calculator are correct. Input requirements: Numbers only; Only one decimal (or no decimals) per input box; No blank input boxes.").ShowAsync();
            }
            finally
            {
                BeginWaitForDb();
                BeginLocalDbBackupOption();
                ShowAllLocalRecords();

                if (success)
                {
                    savedPopupStory.Begin();
                    SaveGlobalAsync(recordToSave);
                }
            }
        }

        async void SaveGlobalAsync(MAFcalculation recordToSave)
        {
            JsonObject jsonObject = new JsonObject();
            jsonObject["year"] = JsonValue.CreateStringValue(recordToSave.Year);
            jsonObject["make"] = JsonValue.CreateStringValue(recordToSave.Make);
            jsonObject["model"] = JsonValue.CreateStringValue(recordToSave.Model);
            jsonObject["engine"] = JsonValue.CreateStringValue(recordToSave.Engine);
            jsonObject["condition"] = JsonValue.CreateStringValue(recordToSave.Condition);
            jsonObject["comments"] = JsonValue.CreateStringValue(recordToSave.Comments);
            jsonObject["mafunits"] = JsonValue.CreateStringValue(recordToSave.MAF_units);
            jsonObject["tempunits"] = JsonValue.CreateStringValue(recordToSave.Temp_units);
            jsonObject["altitudeunits"] = JsonValue.CreateStringValue(recordToSave.Altitude_units);
            jsonObject["rpm"] = JsonValue.CreateNumberValue(recordToSave.Engine_speed);
            jsonObject["maf"] = JsonValue.CreateNumberValue(recordToSave.MAF);
            jsonObject["airtemp"] = JsonValue.CreateNumberValue(recordToSave.Air_temperature);
            jsonObject["altitude"] = JsonValue.CreateNumberValue(recordToSave.Altitude);
            jsonObject["expectedmaf"] = JsonValue.CreateNumberValue(recordToSave.Expected_MAF);
            jsonObject["mafdiff"] = JsonValue.CreateNumberValue(recordToSave.MAF_Difference);
            jsonObject["ve"] = JsonValue.CreateNumberValue(recordToSave.Volumetric_Efficiency);

            string jsonString = jsonObject.Stringify();

            using (HttpClient httpClient = new HttpClient())
            {
                Uri UriToPostTo = new Uri(webAddresses.InsertUrl);
                httpClient.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new HttpContentCodingWithQualityHeaderValue("utf-8"));

                try
                {
                    HttpStringContent content = new HttpStringContent(jsonString, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(UriToPostTo, content);
                }
                catch
                {

                }
                finally
                {
                    ShowRecentGlobalRecords();
                }
            }
        }

        #endregion

        #region Record click

        private void records_ItemClick(object sender, ItemClickEventArgs e)
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

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            _numberOfSearchesFinished = 0;
            progressGlobal.Visibility = Visibility.Visible;
            BeginWaitForDb();

            Search();
        }

        async void Search()
        {
            LocalCollection.Clear();
            noResults.Visibility = Visibility.Collapsed;
            noResultsGlobal.Visibility = Visibility.Collapsed;

            string YEAR = "";
            string MAKE = "";
            string MODEL = "";
            string ENGINE = "";
            string CONDITION = "";
            string COMMENTS = "";
            List<string> queryStringList = new List<string>();
            List<string> searchedForList = new List<string>();
            JsonObject jsonObjectForGlobalSearch = new JsonObject();
            string queryString;

            try
            {
                if (year.SelectedItem != null)
                {
                    YEAR = year.SelectedItem.ToString();
                    searchedForList.Add(YEAR);

                    if (YEAR != "")
                    {
                        jsonObjectForGlobalSearch["year"] = JsonValue.CreateStringValue(YEAR);
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
                        jsonObjectForGlobalSearch["make"] = JsonValue.CreateStringValue(MAKE);
                        MAKE = " Make = '" + MAKE + "'";
                        queryStringList.Add(MAKE);
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.Text))
                {
                    MODEL = model.Text.Trim();
                    searchedForList.Add(MODEL);
                    jsonObjectForGlobalSearch["model"] = JsonValue.CreateStringValue(MODEL);

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
                        jsonObjectForGlobalSearch["engine"] = JsonValue.CreateStringValue(ENGINE);
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
                        jsonObjectForGlobalSearch["condition"] = JsonValue.CreateStringValue(CONDITION);
                        CONDITION = " Condition = '" + CONDITION + "'";
                        queryStringList.Add(CONDITION);
                    }
                }

                if (!string.IsNullOrWhiteSpace(comments.Text))
                {
                    COMMENTS = comments.Text.Trim();

                    // Split comments into individual words and remove separators like commas and white space
                    string[] separators = { ",", ".", "!", "?", ";", ":", " ", "|", "-", "\"", "\\", "/", "%", "*", "(", ")", "+" };
                    string[] words = COMMENTS.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    // Add up to 3 keywords to global search object first
                    var wordCount = words.Count();
                    if (wordCount > 0)
                    {
                        jsonObjectForGlobalSearch["keyword1"] = JsonValue.CreateStringValue(words[0]);

                        if (wordCount > 1)
                        {
                            jsonObjectForGlobalSearch["keyword2"] = JsonValue.CreateStringValue(words[1]);

                            if (wordCount > 2)
                            {
                                jsonObjectForGlobalSearch["keyword3"] = JsonValue.CreateStringValue(words[2]);
                            }
                        }
                    }

                    // Add all keywords to local search
                    foreach (var word in words)
                    {
                        string KEYWORD = " Comments LIKE '%" + word + "%'";
                        queryStringList.Add(KEYWORD);

                        searchedForList.Add(word);
                    }
                }

                if (queryStringList.Count > 0)
                {
                    // Set searchedForText
                    var searchText = string.Join(" ", searchedForList);
                    searchedForText.Text = searchText;
                    searchedForPanelStory.Begin();
                    mafChartDataDescription.Text = searchText;
                    veChartDataDescription.Text = searchText;
                    searchedForTextGlobal.Text = searchText;

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
                    Log("QueryLocalDatabase");
                    LocalCollection = await Task.Run(() => QueryLocalDatabase(queryString));
                    LoadLocalRecords(LocalCollection, localRecords);

                    // Manage noResults visibility
                    if (localRecords.Items.Count == 0)
                    {
                        noResults.Visibility = Visibility.Visible;
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
            finally
            {
                _numberOfSearchesFinished++;
                NumberOfSearchesCompleted = _numberOfSearchesFinished;
                SearchGlobalDatabaseAsync(jsonObjectForGlobalSearch);
                EndWaitForDb();
            }
        }

        async void SearchGlobalDatabaseAsync(JsonObject jsonObject)
        {
            GlobalCollection.Clear();

            string jsonString = jsonObject.Stringify();

            using (HttpClient httpClient = new HttpClient())
            {
                Uri UriToPostTo = new Uri(webAddresses.QueryUrl);
                httpClient.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new HttpContentCodingWithQualityHeaderValue("utf-8"));
                string JSON = "";
                bool success = false;

                try
                {
                    HttpStringContent content = new HttpStringContent(jsonString, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(UriToPostTo, content);

                    JSON = await response.Content.ReadAsStringAsync();

                    if (JSON.Contains("notice"))
                    {
                        globalRecords.Items.Clear();
                        noResultsGlobal.Text = JSON;
                        noResultsGlobal.Visibility = Visibility.Visible;
                        progressGlobal.Visibility = Visibility.Collapsed;
                        _numberOfSearchesFinished++;
                        NumberOfSearchesCompleted = _numberOfSearchesFinished;
                        return;
                    }

                    // Parse JSON
                    JsonArray jsonArray;
                    bool IsParsed = JsonArray.TryParse(JSON, out jsonArray);
                    if (IsParsed)
                    {
                        // Build collection
                        foreach (var item in jsonArray)
                        {
                            var jString = item.ToString();
                            GlobalCollection.Add(new MAFcalculation(jString));
                        }

                        success = true;
                    }
                    else
                    {
                        success = false;
                    }
                }
                catch
                {
                    globalRecords.Items.Clear();
                    noResultsGlobal.Text = "Problem searching global database.\r\nCheck internet connection or try again later.";
                    noResultsGlobal.Visibility = Visibility.Visible;
                }

                globalScrollViewer.ChangeView(0.0, 0.0, null, true);

                if (success)
                {
                    noResultsGlobal.Text = "No results found.";
                    if (GlobalCollection.Count == 0)
                    {
                        noResultsGlobal.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LoadGlobalRecords(GlobalCollection, globalRecords);
                        noResultsGlobal.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    globalRecords.Items.Clear();
                    noResultsGlobal.Text = "Trouble searching global database.\r\nCheck internet connection or try again later.";
                    noResultsGlobal.Visibility = Visibility.Visible;
                }

                searchedForPanelStoryGlobal.Begin();
                progressGlobal.Visibility = Visibility.Collapsed;
                _numberOfSearchesFinished++;
                NumberOfSearchesCompleted = _numberOfSearchesFinished;
            }
        }

        #endregion

        #region Plot data

        void PlotDataOnCharts(List<MAFcalculation> _localRecords, List<MAFcalculation> _globalRecords)
        {
            if (_globalRecords == null)
            {
                if (_localRecords.Count > 0)
                {
                    //plot local only
                    AddCurrentCalculationToList(_localRecords);
                    ConvertToGramsPerSecond(_localRecords);
                    var rpmMinMax = GetMinMaxRpm(_localRecords);
                    var mafMinMax = GetMinMaxMaf(_localRecords);
                    var veMinMax = GetMinMaxVe(_localRecords);
                    SetRpmScaleOnCharts(rpmMinMax);
                    SetMafScaleOnChart(mafMinMax);
                    SetVeScaleOnChart(veMinMax);
                    SetAmountsPerPixel();
                    AddMafDataPlots(_localRecords, localMafPlot);
                    AddVeDataPlots(_localRecords, localVePlot);
                    RemoveLastCalculationFromList(_localRecords);
                }
            }
            else
            {
                if (_globalRecords.Count > 0)
                {
                    if (_localRecords.Count > 0)
                    {
                        //plot both local and global
                        AddCurrentCalculationToList(_localRecords);
                        AddCurrentCalculationToList(_globalRecords);
                        ConvertToGramsPerSecond(_localRecords);
                        ConvertToGramsPerSecond(_globalRecords);
                        var rpmMinMax = GetMinMaxRpm(_localRecords, _globalRecords);
                        var mafMinMax = GetMinMaxMaf(_localRecords, _globalRecords);
                        var veMinMax = GetMinMaxVe(_localRecords, _globalRecords);
                        SetRpmScaleOnCharts(rpmMinMax);
                        SetMafScaleOnChart(mafMinMax);
                        SetVeScaleOnChart(veMinMax);
                        SetAmountsPerPixel();
                        AddMafDataPlots(_localRecords, localMafPlot);
                        AddVeDataPlots(_localRecords, localVePlot);
                        AddMafDataPlots(_globalRecords, globalMafPlot);
                        AddVeDataPlots(_globalRecords, globalVePlot);
                        RemoveLastCalculationFromList(_localRecords);
                        RemoveLastCalculationFromList(_globalRecords);
                    }
                    else
                    {
                        //plot global only
                        AddCurrentCalculationToList(_globalRecords);
                        ConvertToGramsPerSecond(_globalRecords);
                        var rpmMinMax = GetMinMaxRpm(_globalRecords);
                        var mafMinMax = GetMinMaxMaf(_globalRecords);
                        var veMinMax = GetMinMaxVe(_globalRecords);
                        SetRpmScaleOnCharts(rpmMinMax);
                        SetMafScaleOnChart(mafMinMax);
                        SetVeScaleOnChart(veMinMax);
                        SetAmountsPerPixel();
                        AddMafDataPlots(_globalRecords, globalMafPlot);
                        AddVeDataPlots(_globalRecords, globalVePlot);
                        RemoveLastCalculationFromList(_globalRecords);
                    }
                }
                else
                {
                    if (_localRecords.Count > 0)
                    {
                        //plot local only
                        AddCurrentCalculationToList(_localRecords);
                        ConvertToGramsPerSecond(_localRecords);
                        var rpmMinMax = GetMinMaxRpm(_localRecords);
                        var mafMinMax = GetMinMaxMaf(_localRecords);
                        var veMinMax = GetMinMaxVe(_localRecords);
                        SetRpmScaleOnCharts(rpmMinMax);
                        SetMafScaleOnChart(mafMinMax);
                        SetVeScaleOnChart(veMinMax);
                        SetAmountsPerPixel();
                        AddMafDataPlots(_localRecords, localMafPlot);
                        AddVeDataPlots(_localRecords, localVePlot);
                        RemoveLastCalculationFromList(_localRecords);
                    }
                }
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
            mafLocalCheckBox.IsChecked = true;
            mafGlobalCheckBox.IsChecked = true;
            veLocalCheckBox.IsChecked = true;
            veGlobalCheckBox.IsChecked = true;

            localVePlot.Children.Clear();
            localMafPlot.Children.Clear();
            globalVePlot.Children.Clear();
            globalMafPlot.Children.Clear();
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

        double[] GetMinMaxRpm(List<MAFcalculation> _localRecords, List<MAFcalculation> _globalRecords)
        {
            double minLocalRpm = _localRecords.Min(p => p.Engine_speed);
            double maxLocalRpm = _localRecords.Max(p => p.Engine_speed);
            double minGlobalRpm = _globalRecords.Min(p => p.Engine_speed);
            double maxGlobalRpm = _globalRecords.Max(p => p.Engine_speed);

            var min = Math.Min(minLocalRpm, minGlobalRpm);
            var max = Math.Max(maxLocalRpm, maxGlobalRpm);

            double[] _min_max = { min, max };

            return _min_max;
        }

        double[] GetMinMaxRpm(List<MAFcalculation> records)
        {
            double min = records.Min(p => p.Engine_speed);
            double max = records.Max(p => p.Engine_speed);

            double[] _min_max = { min, max };

            return _min_max;
        }

        double[] GetMinMaxMaf(List<MAFcalculation> _localRecords, List<MAFcalculation> _globalRecords)
        {
            double minLocalMaf = _localRecords.Min(p => p.MAF);
            double maxLocalMaf = _localRecords.Max(p => p.MAF);
            double minGlobalMaf = _globalRecords.Min(p => p.MAF);
            double maxGlobalMaf = _globalRecords.Max(p => p.MAF);

            var min = Math.Min(minLocalMaf, minGlobalMaf);
            var max = Math.Max(maxLocalMaf, maxGlobalMaf);

            double[] _min_max = { min, max };

            return _min_max;
        }

        double[] GetMinMaxMaf(List<MAFcalculation> records)
        {
            double min = records.Min(p => p.MAF);
            double max = records.Max(p => p.MAF);

            double[] _min_max = { min, max };

            return _min_max;
        }

        double[] GetMinMaxVe(List<MAFcalculation> _localRecords, List<MAFcalculation> _globalRecords)
        {
            double minLocalVe = _localRecords.Min(p => p.Volumetric_Efficiency);
            double maxLocalVe = _localRecords.Max(p => p.Volumetric_Efficiency);
            double minGlobalVe = _globalRecords.Min(p => p.Volumetric_Efficiency);
            double maxGlobalVe = _globalRecords.Max(p => p.Volumetric_Efficiency);

            var min = Math.Min(minLocalVe, minGlobalVe);
            var max = Math.Max(maxLocalVe, maxGlobalVe);

            double[] _min_max = { min, max };

            return _min_max;
        }

        double[] GetMinMaxVe(List<MAFcalculation> records)
        {
            double min = records.Min(p => p.Volumetric_Efficiency);
            double max = records.Max(p => p.Volumetric_Efficiency);

            double[] _min_max = { min, max };

            return _min_max;
        }

        void SetRpmScaleOnCharts(double[] MinMax)
        {
            double minRecordRpm = MinMax[0];
            double maxRecordRpm = MinMax[1];

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

            var rpmPerDiv = (highRPM - lowRPM) / 5;
            var rpm2 = Math.Round(lowRPM + rpmPerDiv);
            var rpm3 = Math.Round(rpm2 + rpmPerDiv);
            var rpm4 = Math.Round(rpm3 + rpmPerDiv);
            var rpm5 = Math.Round(rpm4 + rpmPerDiv);

            lowMafRpm.Text = lowRPM.ToString();
            highMafRpm.Text = highRPM.ToString();
            mafRpm2.Text = rpm2.ToString();
            mafRpm3.Text = rpm3.ToString();
            mafRpm4.Text = rpm4.ToString();
            mafRpm5.Text = rpm5.ToString();

            lowVeRpm.Text = lowMafRpm.Text;
            highVeRpm.Text = highMafRpm.Text;
            veRpm2.Text = mafRpm2.Text;
            veRpm3.Text = mafRpm3.Text;
            veRpm4.Text = mafRpm4.Text;
            veRpm5.Text = mafRpm5.Text;
        }

        void SetMafScaleOnChart(double[] MinMax)
        {
            double minRecordMaf = MinMax[0];
            double maxRecordMaf = MinMax[1];

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

            var mafPerDiv = (highMAF - lowMAF) / 5;
            var maf2 = Math.Round(lowMAF + mafPerDiv);
            var maf3 = Math.Round(maf2 + mafPerDiv);
            var maf4 = Math.Round(maf3 + mafPerDiv);
            var maf5 = Math.Round(maf4 + mafPerDiv);

            lowMafFlow.Text = lowMAF.ToString();
            highMafFlow.Text = highMAF.ToString();
            mafFlow2.Text = maf2.ToString();
            mafFlow3.Text = maf3.ToString();
            mafFlow4.Text = maf4.ToString();
            mafFlow5.Text = maf5.ToString();
        }

        void SetVeScaleOnChart(double[] MinMax)
        {
            double minRecordVe = MinMax[0];
            double maxRecordVe = MinMax[1];

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

            var vePerDiv = (highVE - lowVE) / 5;
            var ve2 = Math.Round(lowVE + vePerDiv);
            var ve3 = Math.Round(ve2 + vePerDiv);
            var ve4 = Math.Round(ve3 + vePerDiv);
            var ve5 = Math.Round(ve4 + vePerDiv);

            lowVePercent.Text = lowVE.ToString();
            highVePercent.Text = highVE.ToString();
            vePercent2.Text = ve2.ToString();
            vePercent3.Text = ve3.ToString();
            vePercent4.Text = ve4.ToString();
            vePercent5.Text = ve5.ToString();
        }

        void SetAmountsPerPixel()
        {
            rpmPerPixel = 380 / (highRPM - lowRPM);
            mafPerPixel = 380 / (highMAF - lowMAF);
            vePerPixel = 380 / (highVE - lowVE);
        }

        void AddMafDataPlots(List<MAFcalculation> records, Grid chart)
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

                chart.Children.Add(ellipse);
            }
        }

        void AddVeDataPlots(List<MAFcalculation> records, Grid chart)
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

                chart.Children.Add(ellipse);
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

        void AddCurrentCalculationToList(List<MAFcalculation> records)
        {
            if (string.IsNullOrWhiteSpace(maf.Text) || string.IsNullOrWhiteSpace(VE.Text) || string.IsNullOrWhiteSpace(rpm.Text))
            {
                return;
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
            }
        }

        void RemoveLastCalculationFromList(List<MAFcalculation> records)
        {
            records.RemoveAt(records.Count - 1);
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

        #region Charts view Expand/Shrink

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

        #region Shortcut Keys

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            var focusedElement = FocusManager.GetFocusedElement();

            if (focusedElement is TextBox) // Only allow some shortcut keys to be handled, because a TextBox has some of the same shortcut keys and only one thing should handle the shortcut action.
            {
                var currentStateOfCtrlKey = sender.GetAsyncKeyState(VirtualKey.Control);

                if (currentStateOfCtrlKey == CoreVirtualKeyStates.Down)
                {
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.Up: backgroundImageLogPanel.Visibility = Visibility.Visible; break;
                        case VirtualKey.Down: backgroundImageLogPanel.Visibility = Visibility.Collapsed; break;
                        //case VirtualKey.Y: Redo(); break;
                        //case VirtualKey.S: Save(); break;
                        //case VirtualKey.N: New(); break;
                        //case VirtualKey.O: Open(); break;
                        //case VirtualKey.P: Print(); break;
                    }
                }
            }
            else // Allow all global shortcut keys
            {
                var currentStateOfCtrlKey = sender.GetAsyncKeyState(VirtualKey.Control);

                if (currentStateOfCtrlKey == CoreVirtualKeyStates.Down)
                {
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.Up: backgroundImageLogPanel.Visibility = Visibility.Visible; break;
                        case VirtualKey.Down: backgroundImageLogPanel.Visibility = Visibility.Collapsed; break;
                        //case VirtualKey.V: Paste(); break;
                        //case VirtualKey.Z: Undo(); break;
                        //case VirtualKey.Y: Redo(); break;
                        //case VirtualKey.S: Save(); break;
                        //case VirtualKey.N: New(); break;
                        //case VirtualKey.O: Open(); break;
                        //case VirtualKey.C: Copy(); break;
                        //case VirtualKey.P: Print(); break;
                    }
                }
            }
        }

        #endregion

        #region Chart Checkboxes

        private void veLocalCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            localVePlot.Visibility = Visibility.Visible;
        }

        private void veLocalCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            localVePlot.Visibility = Visibility.Collapsed;
        }

        private void veGlobalCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            globalVePlot.Visibility = Visibility.Visible;
        }

        private void veGlobalCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            globalVePlot.Visibility = Visibility.Collapsed;
        }

        private void mafLocalCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            localMafPlot.Visibility = Visibility.Visible;
        }

        private void mafLocalCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            localMafPlot.Visibility = Visibility.Collapsed;
        }

        private void mafGlobalCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            globalMafPlot.Visibility = Visibility.Visible;
        }

        private void mafGlobalCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            globalMafPlot.Visibility = Visibility.Collapsed;
        }
        #endregion

    }
}