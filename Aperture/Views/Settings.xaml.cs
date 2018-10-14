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

            CopyEnabled.IsOn = Settings.ClipboardEnabled;
            CopyEnabled.Toggled += (sender, e) =>
            {
                Settings.ClipboardEnabled = CopyEnabled.IsOn;
            };

            ScanLogEnabled.IsOn = Settings.ScanLogEnabled;
            SetPath.IsEnabled = Settings.ScanLogEnabled;
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
            string token = container.Values[nameof(ScanLogLocation)] as string;
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }
            return await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
        }
    }
}
