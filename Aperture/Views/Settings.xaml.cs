using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using System.IO;
using Windows.Storage.Pickers;
using System.Diagnostics;
using Windows.UI.Xaml;

namespace Aperture
{
    public sealed partial class SettingsPage : Page
    {
        private readonly ApplicationDataContainer settingsContainer = ApplicationData.Current.LocalSettings;

        public SettingsPage()
        {
            this.InitializeComponent();

            SetUp();
        }

        private async void SetUp()
        {
            ServerEnabled.IsOn = Settings.WebSocketsEnabled;
            ServerEnabled.Toggled += (sender, e) =>
            {
                Settings.WebSocketsEnabled = ServerEnabled.IsOn;
            };

            CheckInAction.IsOn = Settings.CheckInAction;
            if (string.IsNullOrEmpty(Settings.AuthCookie))
            {
                CheckInAuthorizationStatus.Text = "Not authorized";
                CheckInAuthorize.Content = "Authorize";
                CheckInEnabled.IsEnabled = false;
                CheckInEnabled.IsOn = false;
                CheckInTag.Text = "";
                CheckInTag.IsEnabled = false;
                CheckInAction.IsEnabled = false;
            }
            else
            {
                CheckInAuthorizationStatus.Text = $"Logged in as {Settings.AuthUsername}";
                CheckInAuthorize.Content = "Log out";
                CheckInEnabled.IsEnabled = true;
                CheckInEnabled.IsOn = Settings.CheckInEnabled;
                CheckInTag.IsEnabled = Settings.CheckInEnabled;
                CheckInAction.IsEnabled = Settings.CheckInEnabled;
                CheckInTag.Text = Settings.CheckInTag ?? "";
            }
            CheckInAuthorize.Click += async (sender, e) =>
            {
                if (string.IsNullOrEmpty(Settings.AuthCookie))
                {
                    // Ask for username / password and log in
                    TextBox usernameBox = new TextBox()
                    {
                        AcceptsReturn = false,
                        Header = "Username",
                        IsSpellCheckEnabled = false,
                    };
                    PasswordBox passwordBox = new PasswordBox()
                    {
                        Header = "Password",
                        Margin = new Thickness(0, 10, 0, 0),
                    };
                    var layout = new StackPanel();
                    layout.Children.Add(usernameBox);
                    layout.Children.Add(passwordBox);

                    ContentDialog dialog = new ContentDialog();
                    dialog.Content = layout;
                    dialog.Title = "HackGT Check-in Login";
                    dialog.IsSecondaryButtonEnabled = true;
                    dialog.PrimaryButtonText = "OK";
                    dialog.SecondaryButtonText = "Cancel";
                    if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                    {
                        CheckInAuthorizationLoading.IsActive = true;
                        if (await CheckInAPI.Login(usernameBox.Text, passwordBox.Password))
                        {
                            CheckInAuthorizationStatus.Text = $"Logged in as {Settings.AuthUsername}";
                            CheckInAuthorize.Content = "Log out";
                            CheckInEnabled.IsEnabled = true;
                            CheckInEnabled.IsOn = Settings.CheckInEnabled;
                            CheckInTag.IsEnabled = Settings.CheckInEnabled;
                            CheckInAction.IsEnabled = Settings.CheckInEnabled;
                            CheckInTag.Text = Settings.CheckInTag ?? "";
                        }
                        else
                        {
                            CheckInAuthorizationStatus.Text = "Couldn't log in";
                        }
                        CheckInAuthorizationLoading.IsActive = false;
                    }
                }
                else
                {
                    // Log out
                    Settings.AuthCookie = null;
                    Settings.AuthUsername = null;
                    CheckInAuthorizationStatus.Text = "Not authorized";
                    CheckInAuthorize.Content = "Authorize";
                    CheckInEnabled.IsEnabled = false;
                    CheckInEnabled.IsOn = false;
                    CheckInTag.Text = "";
                    CheckInTag.IsEnabled = false;
                    CheckInAction.IsEnabled = false;
                }
            };
            CheckInEnabled.Toggled += (sender, e) =>
            {
                Settings.CheckInEnabled = CheckInEnabled.IsOn;
                CheckInTag.IsEnabled = CheckInEnabled.IsOn;
                CheckInAction.IsEnabled = CheckInEnabled.IsOn;
            };
            CheckInTag.TextChanged += (sender, e) =>
            {
                Settings.CheckInTag = CheckInTag.Text;
            };
            CheckInAction.Toggled += (sender, e) =>
            {
                Settings.CheckInAction = CheckInAction.IsOn;
            };

            CopyEnabled.IsOn = Settings.ClipboardEnabled;
            CopyEnabled.Toggled += (sender, e) =>
            {
                Settings.ClipboardEnabled = CopyEnabled.IsOn;
            };

            ScanLogEnabled.IsOn = Settings.ScanLogEnabled && await Settings.GetScanLogLocation() != null;
            SetPath.IsEnabled = ScanLogEnabled.IsOn;
            ScanLogLocation.Text = (await Settings.GetScanLogLocation())?.Path ?? "None set!";
            ScanLogEnabled.Toggled += async (sender, e) =>
            {
                if (ScanLogEnabled.IsOn && (await Settings.GetScanLogLocation()) == null)
                {
                    StorageFile file = await pickSaveFile();
                    if (file == null)
                    {
                        ScanLogEnabled.IsOn = false;
                        return;
                    }
                    Settings.ScanLogLocation = file;
                    ScanLogLocation.Text = file.Path;
                }
                Settings.ScanLogEnabled = ScanLogEnabled.IsOn;
                SetPath.IsEnabled = ScanLogEnabled.IsOn;
            };
            SetPath.Click += async (sender, e) =>
            {
                StorageFile file = await pickSaveFile();
                if (file == null)
                {
                    return;
                }
                Settings.ScanLogLocation = file;
                ScanLogLocation.Text = file.Path;
            };

            Frame rootFrame = Window.Current.Content as Frame;
            MainPage page = rootFrame.Content as MainPage;
            ResetNFC.Click += async (sender, e) =>
            {
                NFCLoading.IsActive = true;
                NFCStatus.Text = "Loading...";
                await page.NFCInit();
                setNFCStatus();
                NFCLoading.IsActive = false;
            };
            setNFCStatus();
        }

        private void setNFCStatus()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            MainPage page = rootFrame.Content as MainPage;
            if (page.nfc.readerFound)
            {
                NFCStatus.Text = "Found and enabled";
            }
            else
            {
                NFCStatus.Text = "No reader detected";
            }
        }

        private async Task<StorageFile> pickSaveFile()
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Log File", new List<string>() { ".log" });
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            savePicker.DefaultFileExtension = ".log";
            savePicker.SuggestedFileName = "badges";
            StorageFile file = await savePicker.PickSaveFileAsync();

            return file;
        }
    }

    public static class Settings
    {
        private static ApplicationDataContainer container = ApplicationData.Current.LocalSettings;

        public static bool WebSocketsEnabled
        {
            get
            {
                return container.Values[nameof(WebSocketsEnabled)] as bool? ?? true;
            }
            set
            {
                container.Values[nameof(WebSocketsEnabled)] = value;
            }
        }
        public static string AuthCookie
        {
            get
            {
                return container.Values[nameof(AuthCookie)] as string;
            }
            set
            {
                container.Values[nameof(AuthCookie)] = value;
            }
        }
        public static string AuthUsername
        {
            get
            {
                return container.Values[nameof(AuthUsername)] as string;
            }
            set
            {
                container.Values[nameof(AuthUsername)] = value;
            }
        }
        public static bool CheckInEnabled
        {
            get
            {
                return container.Values[nameof(CheckInEnabled)] as bool? ?? false;
            }
            set
            {
                container.Values[nameof(CheckInEnabled)] = value;
            }
        }
        public static bool CheckInAction
        {
            get
            {
                return container.Values[nameof(CheckInAction)] as bool? ?? true;
            }
            set
            {
                container.Values[nameof(CheckInAction)] = value;
            }
        }
        public static string CheckInTag
        {
            get
            {
                return container.Values[nameof(CheckInTag)] as string;
            }
            set
            {
                container.Values[nameof(CheckInTag)] = value;
            }
        }
        public static bool ClipboardEnabled
        {
            get
            {
                return container.Values[nameof(ClipboardEnabled)] as bool? ?? false;
            }
            set
            {
                container.Values[nameof(ClipboardEnabled)] = value;
            }
        }
        public static bool ScanLogEnabled
        {
            get
            {
                return container.Values[nameof(ScanLogEnabled)] as bool? ?? false;
            }
            set
            {
                container.Values[nameof(ScanLogEnabled)] = value;
            }
        }

        public static StorageFile ScanLogLocation
        {
            set
            {
                string token = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(value, nameof(ScanLogLocation));
                container.Values[nameof(ScanLogLocation)] = token;
            }
        }
        public async static Task<StorageFile> GetScanLogLocation()
        {
            try
            {
                string token = container.Values[nameof(ScanLogLocation)] as string;
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }
                return await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
