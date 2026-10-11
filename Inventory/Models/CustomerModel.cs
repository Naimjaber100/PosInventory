using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_customer_get_all: a client (customers table).
    // Balance = what the client owes us (sales on account minus payments); it is never typed in.
    public class CustomerModel
    {
        [Browsable(false)] public int CustomerId { get; set; }
        [DisplayName("Name")] public string Name { get; set; } = string.Empty;
        [DisplayName("Phone")] public string? Phone { get; set; }
        [DisplayName("Address")] public string? Address { get; set; }
        [DisplayName("Email")] public string? Email { get; set; }
        [DisplayName("Owes us")] public decimal BalanceLbp { get; set; }
        [DisplayName("Credit limit")] public decimal? CreditLimitLbp { get; set; }   // null = no limit
        [DisplayName("Notes")] public string? Notes { get; set; }
        [DisplayName("Active")] public bool IsActive { get; set; } = true;
    }
}
