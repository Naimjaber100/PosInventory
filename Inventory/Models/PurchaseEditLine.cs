using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Inventory.Models
{
    // One editable line in the purchase add / edit grid. Only Qty and Unit cost can be edited;
    // Line total follows them (PropertyChanged keeps the grid and the totals up to date).
    public class PurchaseEditLine : INotifyPropertyChanged
    {
        private decimal _quantity;
        private decimal _unitCost;

        [Browsable(false)] public int PackagingId { get; init; }
        [Browsable(false)] public int ProductId { get; init; }
        [DisplayName("Item"), ReadOnly(true)] public string ItemName { get; init; } = string.Empty;
        [DisplayName("Packaging"), ReadOnly(true)] public string PackagingName { get; init; } = string.Empty;
        [DisplayName("Barcode"), ReadOnly(true)] public string? Barcode { get; init; }
        [Browsable(false)] public decimal Factor { get; init; } = 1;        // base units in one packaging
        [Browsable(false)] public string BaseUnit { get; init; } = string.Empty;
        [Browsable(false)] public bool IsWeighted { get; init; }           // false = whole base units only

        [DisplayName("Qty")]
        public decimal Quantity
        {
            get => _quantity;
            set { if (_quantity == value) return; _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); }
        }

        [DisplayName("Unit cost")]
        public decimal UnitCost
        {
            get => _unitCost;
            set { if (_unitCost == value) return; _unitCost = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); }
        }

        [DisplayName("Line total")]
        public decimal LineTotal => Math.Round(Quantity * UnitCost, 2);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
