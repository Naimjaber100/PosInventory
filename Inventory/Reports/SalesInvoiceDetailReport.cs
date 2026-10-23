using Inventory.Models;
using Inventory.Printing;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 3b - Sales by invoice with items: like Sales by invoice, with the items of every invoice under it
    // (item, qty, price, discount, total, profit). Returns are negative (red), so the TOTAL row is the net.
    // Double-click an invoice or one of its items: its receipt.
    public sealed class SalesInvoiceDetailReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);
        private static readonly Color InvoiceBack = Color.FromArgb(243, 245, 248);

        public override string Title => "Sales by invoice with items";

        public override string Description =>
            "Every sale and return invoice with its items: qty, price, discount, total and profit. Returns are negative, so the total is the net.";

        public override (DateTime From, DateTime To) DefaultPeriod => (DateTime.Today, DateTime.Today);

        public override string? DoubleClickHint => "Double-click an invoice to see its receipt.";

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var repository = new ReportRepository();
            var invoices = await repository.GetSalesByInvoiceAsync(filter.From, filter.To, filter.UserId);
            var lines = (await repository.GetSalesInvoiceLinesAsync(filter.From, filter.To, filter.UserId)).ToLookup(l => l.SaleId);
            var total = SalesByInvoiceRow.TotalOf(invoices);

            // Newest invoice first; its items stay in the order they were sold
            var rows = new List<SalesInvoiceDetailRow>();
            foreach (var invoice in invoices.OrderByDescending(i => i.SaleId))
            {
                rows.Add(SalesInvoiceDetailRow.ForInvoice(invoice));
                rows.AddRange(lines[invoice.SaleId].Select(l => SalesInvoiceDetailRow.ForLine(l, invoice.IsReturn)));
            }
            rows.Add(SalesInvoiceDetailRow.ForTotal(total));

            var sales = invoices.Where(i => !i.IsReturn).ToList();
            var returns = invoices.Where(i => i.IsReturn).ToList();
            var money = filter.Currencies;
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("TOTAL SALES", money.FormatPrimary(total.Total), Green,
                        money.FormatSecond(total.Total) is { Length: > 0 } s ? $"{s}  ·  returns subtracted" : "returns subtracted"),
                    new("TOTAL PROFIT", money.FormatPrimary(total.Profit), total.Profit < 0 ? Red : Blue,
                        total.Margin is { } m ? $"margin {m:0.#}%  ·  cost {money.FormatPrimary(total.Cost)}" : null),
                    new("INVOICES", $"{sales.Count:#,0}", Dark, $"{sales.Sum(i => i.ItemCount):#,0} item line(s)"),
                    new("RETURNS", money.FormatPrimary(-returns.Sum(i => i.Total)), returns.Count > 0 ? Red : Dark, $"{returns.Count:#,0} return invoice(s)"),
                    new("DISCOUNTS", money.FormatPrimary(sales.Sum(i => i.Discounts)), Orange),
                },
            };
        }

        public override void FormatGrid(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.ValueType is not { } valueType) continue;
                var type = Nullable.GetUnderlyingType(valueType) ?? valueType;
                if (type == typeof(DateTime))
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy  HH:mm";
                    continue;
                }
                if (type != typeof(decimal)) continue;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.Format = column.DataPropertyName == nameof(SalesInvoiceDetailRow.Quantity) ? "#,0.###" : "#,0";
                column.FillWeight = 60;
            }
            if (grid.Columns[nameof(SalesInvoiceDetailRow.InvoiceNo)] is { } no) no.FillWeight = 120;
            if (grid.Columns[nameof(SalesInvoiceDetailRow.InvoiceDate)] is { } date) date.FillWeight = 110;
            if (grid.Columns[nameof(SalesInvoiceDetailRow.ItemName)] is { } item) item.FillWeight = 170;
            if (grid.Columns[nameof(SalesInvoiceDetailRow.Payments)] is { } paid) paid.FillWeight = 120;
            if (grid.Columns[nameof(SalesInvoiceDetailRow.Total)] is { } total) total.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // Invoice rows bold on a light background, item rows plain; returns and losses red, discounts orange
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not SalesInvoiceDetailRow r) return;
            switch (r.Kind)
            {
                case SalesInvoiceDetailRow.RowKind.Total:
                    style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                    style.ForeColor = style.SelectionForeColor = Dark;
                    style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
                    return;
                case SalesInvoiceDetailRow.RowKind.Invoice:
                    style.BackColor = InvoiceBack;
                    style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
                    if (column.DataPropertyName == nameof(SalesInvoiceDetailRow.ItemName)) style.ForeColor = style.SelectionForeColor = Color.DimGray;
                    break;
            }
            if (r.IsReturn) style.ForeColor = style.SelectionForeColor = Red;
            else if (column.DataPropertyName == nameof(SalesInvoiceDetailRow.Profit) && r.Profit < 0) style.ForeColor = style.SelectionForeColor = Red;
            else if (column.DataPropertyName == nameof(SalesInvoiceDetailRow.Discount) && r.Discount > 0) style.ForeColor = style.SelectionForeColor = Orange;
        }

        public override bool IsTotalRow(object row) => row is SalesInvoiceDetailRow { Kind: SalesInvoiceDetailRow.RowKind.Total };

        public override async void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter)
        {
            if (row is not SalesInvoiceDetailRow { Kind: not SalesInvoiceDetailRow.RowKind.Total } r) return;
            try
            {
                var settings = ReceiptSettings.Current ?? await ReceiptSettings.LoadAsync();
                if (await new SaleRepository().GetReceiptAsync(r.SaleId) is { } receipt)
                    ReceiptPrinter.ShowPreview(owner, receipt, filter.Currencies, settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Could not show the invoice:\n" + ex.Message, Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
