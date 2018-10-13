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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Aperture
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            var nfc = new NFC();
            new Task(async () =>
            {
                await nfc.Setup();
                nfc.BadgeTapped += Nfc_BadgeTapped;
            }).Start();

            Navigation.SelectedItem = Portal;
            Navigation.SelectionChanged += Navigation_SelectionChanged;
            Navigation.Loaded += (sender, args) =>
            {
                contentFrame.Navigate(typeof(Main));
            };
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

        private void Nfc_BadgeTapped(object sender, BadgeEventArgs e)
        {
            Debug.WriteLine(e.uuid);
        }
    }
}
