using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.SmartCards;
using Windows.Devices.Enumeration;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NdefLibrary.Ndef;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Aperture
{
    public class BadgeEventArgs: EventArgs
    {
        public string uuid { get; set; }
        public string url { get; set; }

        private readonly Regex urlParser = new Regex(
            @"^https:\/\/live.hack.gt\/?\?user=([a-f0-9\-]+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100)
        );
        
        public BadgeEventArgs(string url)
        {
            this.url = url;
            var match = urlParser.Match(url);
            if (match.Success)
            {
                uuid = match.Groups[1].Value;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", uuid, url);
        }
    }

    public class NFC
    {
        private static SmartCardReader reader;
        public bool readerFound { get { return reader != null; } }
        public event EventHandler<BadgeEventArgs> BadgeTapped;

        protected virtual void OnBadgeTapped(BadgeEventArgs e) => BadgeTapped?.Invoke(this, e);

        public async Task Setup()
        {
            if (reader != null)
            {
                reader.CardAdded -= Reader_CardAdded;
            }

            var devices = await DeviceInformation.FindAllAsync(SmartCardReader.GetDeviceSelector(SmartCardReaderKind.Generic));
            foreach (DeviceInformation device in devices)
            {
                reader = await SmartCardReader.FromIdAsync(device.Id);
            }
            if (reader == null)
            {
                Debug.WriteLine("No NFC reader found");
                return;
            }
            reader.CardAdded += Reader_CardAdded;
        }

        private async void Reader_CardAdded(SmartCardReader sender, CardAddedEventArgs args)
        {
            var cards = await sender.FindAllCardsAsync();
            if (cards.Count < 1) return;
            var card = cards[0];

            const int READ_AMOUNT = 80;

            SmartCardConnection connection;
            try
            {
                connection = await card.ConnectAsync();
            }
            catch (Exception) // This is literally the most specific exception sent for this error
            {
                // Couldn't open reader because tag was lifted too early
                return;
            }
            
            using (connection)
            {
                const byte START_BLOCK = 4;
                const byte MAX_READ_SIZE = 16;

                byte[] data = new byte[READ_AMOUNT];
                for (int i = 0; i < Math.Ceiling((float)READ_AMOUNT / MAX_READ_SIZE); i++)
                {
                    byte[] command = { 0xFF, 0xB0, 0x00, (byte)(START_BLOCK + i * 4), MAX_READ_SIZE };
                    var response = await connection.TransmitAsync(command.AsBuffer());
                    if (response.Length != MAX_READ_SIZE + 2) // 2 bytes for command execution status
                    {
                        Debug.WriteLine("Tag lifted too early");
                        return;
                    }
                    uint length = response.Length;
                    response.CopyTo(0, data, i * MAX_READ_SIZE, MAX_READ_SIZE);
                }

                // Parse TLV blocks
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0x00)
                    {
                        // NULL TLV
                        continue;
                    }
                    else if (data[i] == 0x01)
                    {
                        // Lock control TLV
                        i++;
                        i += data[i]; // Skip over block according to its length
                    }
                    else if (data[i] == 0x03)
                    {
                        // NDEF TLV
                        i++;
                        byte length = data[i];

                        byte[] ndefData = new byte[length];
                        Array.Copy(data, i + 1, ndefData, 0, length);
                        string uri = getURIFromNdef(ndefData);

                        OnBadgeTapped(new BadgeEventArgs(uri));

                        i += length + 1; // Also skip over trailing 0xFE
                    }
                }
            }
        }

        private string getURIFromNdef(byte[] data)
        {
            var message = NdefMessage.FromByteArray(data);
            foreach (NdefRecord record in message)
            {
                if (record.CheckSpecializedType(false) == typeof(NdefUriRecord))
                {
                    var uri = new NdefUriRecord(record);
                    return uri.Uri;
                }
            }
            return null;
        }
    }
}
