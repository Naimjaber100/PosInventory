namespace Inventory.Models
{
    // Row of fn_scale_get_all: a label scale the items are sent to (Settings > Label scales)
    public class ScaleModel
    {
        public int ScaleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Driver { get; set; } = string.Empty;   // Scales.ScaleDrivers key: the model / way of sending
        public string? Host { get; set; }                    // network scales: IP address
        public int? Port { get; set; }
        public string? SerialPort { get; set; }              // serial scales: COM1, COM2...
        public int? BaudRate { get; set; }
        public string? FilePath { get; set; }                // file drivers: the file written for the scale's PC program
        public bool IsActive { get; set; } = true;
        public long ItemCount { get; set; }

        public override string ToString() => Name;
    }

    // Row of fn_scale_item_list: an item loaded in a scale
    public class ScaleItemModel
    {
        public int ProductId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ScaleCode { get; set; }   // the PLU (products.scale_code)
        public int? HotKey { get; set; }         // the key on the scale's keyboard
        public decimal? Price { get; set; }      // null: the item has no packaging that can be sold
        public bool IsWeighted { get; set; }
        public string BaseUnit { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? SentAt { get; set; }
        public string Status { get; set; } = string.Empty;   // New, Changed, Sent
    }
}
