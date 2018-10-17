using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.Storage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Aperture
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public NFC nfc;

        public MainPage()
        {
            this.InitializeComponent();

            nfc = new NFC();
            nfc.BadgeTapped += Nfc_BadgeTapped;

            new Task(async () =>
            {
                await NFCInit();
            }).Start();

            Navigation.SelectedItem = Portal;
            Navigation.SelectionChanged += Navigation_SelectionChanged;
            Navigation.Loaded += (sender, args) =>
            {
                contentFrame.Navigate(typeof(Main));
            };
        }

        public async Task NFCInit()
        {
            await nfc.Setup();
        }

        private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (!args.IsSettingsSelected)
            {
                contentFrame.Navigate(typeof(Main));
            }
            else
            {
                contentFrame.Navigate(typeof(SettingsPage));
            }
        }

        public WebView SportalFrame;
        private async void Nfc_BadgeTapped(object sender, BadgeEventArgs e)
        {
            if (!Settings.CheckInEnabled)
            {
                var toastContent = new ToastContent()
                {
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "Badge scanned"
                                },
                                new AdaptiveText()
                                {
                                    Text = "ID: " + e.uuid
                                },
                            }
                        }
                    }
                };
                var toast = new ToastNotification(toastContent.GetXml())
                {
                    ExpirationTime = DateTime.Now.AddSeconds(10)
                };
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }

            if (Settings.CheckInEnabled)
            {
                string name;
                if (Settings.CheckInAction)
                {
                    name = await CheckInAPI.CheckIn(e.uuid, Settings.CheckInTag);
                }
                else
                {
                    name = await CheckInAPI.CheckOut(e.uuid, Settings.CheckInTag);
                }

                var toastContent = new ToastContent()
                {
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = $"Checked {(Settings.CheckInAction ? "in" : "out")} user"
                                },
                                new AdaptiveText()
                                {
                                    Text = name != null ? $"Name: {name}" : "Invalid tag"
                                },
                            }
                        }
                    }
                };
                var toast = new ToastNotification(toastContent.GetXml())
                {
                    ExpirationTime = DateTime.Now.AddSeconds(10)
                };
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            if (Settings.WebSocketsEnabled)
            {
                // Run on UI thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        await SportalFrame.InvokeScriptAsync("eval", new string[] { $"nfcService.onReceiveID(\"{e.uuid}\")" });
                    }
                    catch (Exception err)
                    {
                        Debug.WriteLine($"{err.Message}: {err.StackTrace}");
                    }
                });
            }
            if (Settings.ClipboardEnabled)
            {
                var package = new DataPackage();
                package.RequestedOperation = DataPackageOperation.Copy;
                package.SetText(e.uuid);

                // Clipboard operations must occur on the UI thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    try
                    {
                        Clipboard.SetContent(package);
                    }
                    catch (Exception err)
                    {
                        Debug.WriteLine($"{err.Message}: {err.StackTrace}");
                    }
                });
            }
            if (Settings.ScanLogEnabled)
            {
                StorageFile logFile = await Settings.GetScanLogLocation();
                await FileIO.AppendTextAsync(logFile, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}] Scanned badge: {e.uuid}\r\n");
            }
        }
    }
}
