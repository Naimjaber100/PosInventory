using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inventory.Printing
{
    // One receipt as returned by fn_sale_receipt (Database/receipt_functions.sql): amounts in the primary currency,
    // payments in the currency they were paid in.
    public sealed class Receipt
    {
        public ReceiptCompany Company { get; set; } = new();
        public string InvoiceNo { get; set; } = string.Empty;
        public short InvoiceType { get; set; } = 1;   // 1 sale, 2 return
        public DateTimeOffset InvoiceDate { get; set; }
        public string? Cashier { get; set; }
        public string? Customer { get; set; }
        public decimal SubTotal { get; set; }
        public decimal LineDiscounts { get; set; }
        public decimal Discount { get; set; }          // invoice discount
        public decimal Total { get; set; }
        public decimal ChangeLbp { get; set; }
        public decimal UsdRate { get; set; }
        public string PrimaryCode { get; set; } = "LBP";
        public List<ReceiptLine> Lines { get; set; } = new();
        public List<ReceiptPayment> Payments { get; set; } = new();

        public bool IsReturn => InvoiceType == 2;

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        public static Receipt? FromJson(string? json) =>
            string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<Receipt>(json, Options);

        /// <summary>A made-up sale for the preview and the test print.</summary>
        public static Receipt Sample(string companyName) => new()
        {
            Company = new ReceiptCompany { Name = companyName, Address = "Hamra street, Beirut", Mobile = "03 123 456", VatNumber = "123456" },
            InvoiceNo = "S-TEST-000001",
            InvoiceDate = DateTimeOffset.Now,
            Cashier = AppSession.UserName,
            SubTotal = 1_185_000, Discount = 10_000, Total = 1_175_000, ChangeLbp = 25_000, UsdRate = 89_500,
            Lines =
            {
                new() { ItemName = "Pepsi 330 ml", Quantity = 6, UnitPrice = 45_000, LineTotal = 270_000 },
                new() { ItemName = "بيض بلدي - كرتونة 30", Quantity = 1, UnitPrice = 360_000, LineTotal = 360_000 },
                new() { ItemName = "Halloumy cheese", Quantity = 0.750m, UnitPrice = 600_000, LineTotal = 450_000 },
                new() { ItemName = "Unica chocolate bar with a very long name to show the wrapping", Quantity = 3, UnitPrice = 35_000, Discount = 0, LineTotal = 105_000 },
            },
            Payments = { new() { Method = 1, Currency = "USD", Amount = 10, AmountLbp = 895_000 }, new() { Method = 1, Currency = "LBP", Amount = 305_000, AmountLbp = 305_000 } },
        };
    }

    public sealed class ReceiptCompany
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? VatNumber { get; set; }
    }

    public sealed class ReceiptLine
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public sealed class ReceiptPayment
    {
        public short Method { get; set; }   // 1 cash, 2 card, 3 on account
        public string Currency { get; set; } = "LBP";
        public decimal Amount { get; set; }
        public decimal AmountLbp { get; set; }
    }
}
