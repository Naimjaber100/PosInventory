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
    }
}
