using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Inventory.Models
{
    // One line on the cashier screen. Quantity, price and the discounts can change;
    // PropertyChanged keeps the grid and the totals up to date.
    public class PosLine : INotifyPropertyChanged
    {
        private decimal _quantity = 1;
        private decimal _unitPrice;
        private decimal _discount;
        private decimal _promoDiscount;

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

        /// <summary>Discount amount typed by the cashier (F5), on the whole line (not per unit).</summary>
        [Browsable(false)]
        public decimal Discount
        {
            get => _discount;
            set { if (_discount == value) return; _discount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalDiscount)); OnPropertyChanged(nameof(LineTotal)); }
        }

        /// <summary>What the promotions take off the whole line; set by the cashier screen (PromotionEngine).</summary>
        [Browsable(false)]
        public decimal PromoDiscount
        {
            get => _promoDiscount;
            set { if (_promoDiscount == value) return; _promoDiscount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalDiscount)); OnPropertyChanged(nameof(LineTotal)); }
        }

        /// <summary>The promotion(s) PromoDiscount comes from.</summary>
        [Browsable(false)] public string? PromoName { get; set; }

        [DisplayName("Disc.")]
        public decimal TotalDiscount => Discount + PromoDiscount;

        [DisplayName("Total")]
        public decimal LineTotal => Math.Round(Quantity * UnitPrice - TotalDiscount, 2);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
