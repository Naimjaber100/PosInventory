namespace Inventory.Models
{
    // Row returned by fn_company_get: the one company record (Settings > Company)
    public class CompanyModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? VatNumber { get; set; }

        // TVA: charged or not, whether the items' sale prices already include it, and the rate (%) a new item starts with
        public bool VatEnabled { get; set; }
        public bool PricesIncludeVat { get; set; } = true;
        public decimal VatRate { get; set; } = 11;
    }
}
