using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_supplier_get_all: a supplier (suppliers table).
    // Balance = what we owe the supplier (posted purchases minus payments); it is never typed in.
    public class SupplierModel
    {
        [Browsable(false)] public int SupplierId { get; set; }
        [DisplayName("Name")] public string Name { get; set; } = string.Empty;
        [DisplayName("Phone")] public string? Phone { get; set; }
        [DisplayName("Address")] public string? Address { get; set; }
        [DisplayName("Email")] public string? Email { get; set; }
        [DisplayName("VAT number")] public string? VatNumber { get; set; }
        [DisplayName("We owe")] public decimal BalanceLbp { get; set; }
        [DisplayName("Notes")] public string? Notes { get; set; }
        [DisplayName("Active")] public bool IsActive { get; set; } = true;
    }
}
