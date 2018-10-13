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

namespace Aperture
{
    public sealed partial class SettingsPage : Page
    {
        private readonly ApplicationDataContainer settingsContainer = ApplicationData.Current.LocalSettings;

        public SettingsPage()
        {
            this.InitializeComponent();

            ServerEnabled.IsOn = Settings.WebSocketsEnabled;
            ServerPort.IsEnabled = Settings.WebSocketsEnabled;
            ServerPort.Text = Settings.WebSocketsPort.ToString();
            ServerEnabled.Toggled += (sender, e) =>
            {
                ServerPort.IsEnabled = ServerEnabled.IsOn;
                Settings.WebSocketsEnabled = ServerEnabled.IsOn;
            };
            ServerPort.LostFocus += (sender, e) =>
            {
                short parsedPort = Settings.WebSocketsPort;
                short.TryParse(ServerPort.Text, out parsedPort);
                Settings.WebSocketsPort = parsedPort;
                ServerPort.Text = parsedPort.ToString();
            };

            CopyEnabled.IsOn = Settings.ClipboardEnabled;
            CopyEnabled.Toggled += (sender, e) =>
            {
                Settings.ClipboardEnabled = CopyEnabled.IsOn;
            };

            ScanLogEnabled.IsOn = Settings.ScanLogEnabled;
            SetPath.IsEnabled = Settings.ScanLogEnabled;
            ScanLogLocation.Text = Settings.ScanLogLocation ?? "None set!";
            ScanLogEnabled.Toggled += async (sender, e) =>
            {
                if (ScanLogEnabled.IsOn && Settings.ScanLogLocation == null)
                {
                    string path = await pickSaveFile();
                    if (string.IsNullOrEmpty(path))
                    {
                        ScanLogEnabled.IsOn = false;
                        return;
                    }
                    Settings.ScanLogLocation = path;
                    ScanLogLocation.Text = path;
                }
                Settings.ScanLogEnabled = ScanLogEnabled.IsOn;
                SetPath.IsEnabled = ScanLogEnabled.IsOn;
            };
            SetPath.Click += async (sender, e) =>
            {
                string path = await pickSaveFile();
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                Settings.ScanLogLocation = path;
                ScanLogLocation.Text = path;
            };
        }

        private async Task<string> pickSaveFile()
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Log File", new List<string>() { ".log" });
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            savePicker.DefaultFileExtension = ".log";
            savePicker.SuggestedFileName = "badges";
            StorageFile file = await savePicker.PickSaveFileAsync();

            return file?.Path;
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
        public static short WebSocketsPort
        {
            get
            {
                return container.Values[nameof(WebSocketsPort)] as short? ?? 1337;
            }
            set
            {
                container.Values[nameof(WebSocketsPort)] = value;
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
        public static string ScanLogLocation
        {
            get
            {
                return container.Values[nameof(ScanLogLocation)] as string;
            }
            set
            {
                container.Values[nameof(ScanLogLocation)] = value;
            }
        }
    }
}
