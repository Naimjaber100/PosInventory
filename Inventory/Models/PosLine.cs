using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Inventory.Models
{
    // One line on the cashier screen. Quantity, price and discount can change;
    // PropertyChanged keeps the grid and the totals up to date.
    public class PosLine : INotifyPropertyChanged
    {
        private decimal _quantity = 1;
        private decimal _unitPrice;
        private decimal _discount;

        [Browsable(false)] public int PackagingId { get; init; }
        [Browsable(false)] public int ProductId { get; init; }
        [DisplayName("Item"), ReadOnly(true)] public string ItemName { get; init; } = string.Empty;
        [Browsable(false)] public string? Barcode { get; init; }
        [Browsable(false)] public decimal Factor { get; init; } = 1;
        [Browsable(false)] public bool IsWeighted { get; init; }
        [Browsable(false)] public string BaseUnit { get; init; } = string.Empty;
        [Browsable(false)] public decimal StockQty { get; init; }

        [DisplayName("Qty"), ReadOnly(true)]
        public decimal Quantity
        {
            get => _quantity;
            set { if (_quantity == value) return; _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); }
        }

        [DisplayName("Price"), ReadOnly(true)]
        public decimal UnitPrice
        {
            get => _unitPrice;
            set { if (_unitPrice == value) return; _unitPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); }
        }

        /// <summary>Discount amount on the whole line (not per unit).</summary>
        [DisplayName("Disc."), ReadOnly(true)]
        public decimal Discount
        {
            get => _discount;
            set { if (_discount == value) return; _discount = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); }
        }

        [DisplayName("Total")]
        public decimal LineTotal => Math.Round(Quantity * UnitPrice - Discount, 2);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
