using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using Inventory.Models;

namespace Inventory
{
    // What the cashier screen tells the customer's display, whatever kind it is (CustomerDisplay.Type in appsettings.json):
    //   "Screen": a second monitor (CustomerDisplayForm)
    //   "Pole":   a pole display (VFD, 2 lines of 20 characters) on a COM port / USB (PoleDisplay)
    public interface ICustomerDisplay
    {
        void ShowItem(PosLine line);
        void ShowTotals(int lineCount, decimal discount, decimal total, CurrencySet currencies, bool isReturn);
        void ShowThanks(decimal total, decimal? change, CurrencySet currencies);
        void Close();
    }

    public static class CustomerDisplays
    {
        /// <summary>The display of this POS station; null when it is turned off or not found.</summary>
        public static ICustomerDisplay? Open(IBindingList lines) =>
            !AppConfig.CustomerDisplay.Enabled ? null
            : string.Equals(AppConfig.CustomerDisplay.Type, "Pole", StringComparison.OrdinalIgnoreCase) ? PoleDisplay.Open(AppConfig.CustomerDisplay)
            : CustomerDisplayForm.Open(lines);
    }

    // A pole display (VFD) with 2 lines: USB ones show up as a COM port (Windows Device Manager > Ports).
    //   no sale  ->  "Welcome" / shop name
    //   an item  ->  item name ........ line total  /  TOTAL ........ total
    //   paid     ->  TOTAL ........ total  /  CHANGE ........ change
    // Commands: "EscPos" (Epson ESC/POS customer display, the usual one) or "CD5220". A display that is unplugged
    // or busy never stops a sale: the text is skipped and the port is opened again at the next change.
    // Only Latin letters show on these displays: other characters are sent as "?".
    public sealed class PoleDisplay : ICustomerDisplay
    {
        private readonly CustomerDisplaySettings _settings;
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
        private SerialPort? _port;
        private string _itemLine = string.Empty, _totalLine = string.Empty;
        private DateTime? _thanksUntil;
        private string _storeName = string.Empty;

        private int Width => Math.Clamp(_settings.LineWidth, 8, 40);

        public static PoleDisplay? Open(CustomerDisplaySettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Port)) return null;
            var display = new PoleDisplay(settings);
            display.ShowWelcome();
            _ = display.LoadStoreNameAsync();
            return display;
        }

        private PoleDisplay(CustomerDisplaySettings settings)
        {
            _settings = settings;
            _timer.Tick += (_, _) =>
            {
                if (_thanksUntil is { } until && DateTime.Now >= until) ShowWelcome();
            };
            _timer.Start();
        }

        private async Task LoadStoreNameAsync()
        {
            try
            {
                _storeName = await new Repositories.CompanyRepository().GetNameAsync();
                if (_thanksUntil == null && _itemLine.Length == 0) ShowWelcome();
            }
            catch { /* no name: "Welcome" alone */ }
        }

        public void ShowWelcome()
        {
            _thanksUntil = null;
            _itemLine = _totalLine = string.Empty;
            Write(Center(_settings.WelcomeText), Center(_storeName));
        }

        public void ShowItem(PosLine line)
        {
            _itemLine = LeftRight(line.ItemName, line.LineTotal.ToString("#,0"));
            if (_thanksUntil == null && _totalLine.Length > 0) Write(_itemLine, _totalLine);
        }

        // Called after every change of the sale; when an item is scanned ShowItem comes right after it
        public void ShowTotals(int lineCount, decimal discount, decimal total, CurrencySet currencies, bool isReturn)
        {
            if (lineCount == 0)
            {
                if (_thanksUntil == null) ShowWelcome();
                return;
            }
            _thanksUntil = null;
            _totalLine = LeftRight(isReturn ? "REFUND" : "TOTAL", total.ToString("#,0"));
            Write(_itemLine, _totalLine);
        }

        public void ShowThanks(decimal total, decimal? change, CurrencySet currencies)
        {
            _itemLine = _totalLine = string.Empty;
            Write(LeftRight("TOTAL", total.ToString("#,0")),
                change is > 0 ? LeftRight("CHANGE", change.Value.ToString("#,0")) : Center(_settings.ThankYouText));
            _thanksUntil = DateTime.Now.AddSeconds(Math.Max(3, _settings.ThankYouSeconds));
        }

        public void Close()
        {
            _timer.Dispose();
            try
            {
                if (_port?.IsOpen == true) Send(Clear());
                _port?.Dispose();
            }
            catch { /* closing anyway */ }
            _port = null;
        }

        #region Text

        // "HALLOUMI      37,500": the amount always shows, the name is cut to make room
        private string LeftRight(string left, string right)
        {
            right = right.Length > Width ? right[^Width..] : right;
            var room = Width - right.Length - 1;
            left = room <= 0 ? string.Empty : left.Length > room ? left[..room] : left;
            return left.PadRight(Width - right.Length) + right;
        }

        private string Center(string text)
        {
            text = text.Trim();
            if (text.Length >= Width) return text[..Width];
            return text.PadLeft((Width + text.Length) / 2).PadRight(Width);
        }

        #endregion

        #region Port and commands

        private bool CD5220 => string.Equals(_settings.Commands, "CD5220", StringComparison.OrdinalIgnoreCase);

        // ESC/POS: ESC @ (reset), CLR. CD5220: ESC @, then CLR as well.
        private static byte[] Clear() => new byte[] { 0x1B, 0x40, 0x0C };

        // Both lines, each padded to the full width so nothing of the old text stays
        private void Write(string line1, string line2)
        {
            line1 = line1.PadRight(Width)[..Width];
            line2 = line2.PadRight(Width)[..Width];
            var bytes = new List<byte>();
            if (CD5220)
            {
                // ESC Q A text CR = upper line, ESC Q B text CR = lower line
                bytes.AddRange(new byte[] { 0x1B, 0x51, 0x41 }); bytes.AddRange(Ascii(line1)); bytes.Add(0x0D);
                bytes.AddRange(new byte[] { 0x1B, 0x51, 0x42 }); bytes.AddRange(Ascii(line2)); bytes.Add(0x0D);
            }
            else
            {
                // US $ x y = cursor to column x, line y (from 1)
                bytes.AddRange(new byte[] { 0x1F, 0x24, 1, 1 }); bytes.AddRange(Ascii(line1));
                bytes.AddRange(new byte[] { 0x1F, 0x24, 1, 2 }); bytes.AddRange(Ascii(line2));
            }
            Send(bytes.ToArray());
        }

        private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

        private void Send(byte[] bytes)
        {
            try
            {
                if (_port is not { IsOpen: true })
                {
                    _port?.Dispose();
                    _port = new SerialPort(_settings.Port!.Trim(), _settings.BaudRate, Parity.None, 8, StopBits.One) { WriteTimeout = 500 };
                    _port.Open();
                    _port.Write(Clear(), 0, 3);
                }
                _port.Write(bytes, 0, bytes.Length);
            }
            catch
            {
                // Unplugged, wrong port or busy: the sale goes on; the next change tries again
                try { _port?.Dispose(); } catch { /* already gone */ }
                _port = null;
            }
        }

        #endregion
    }
}
