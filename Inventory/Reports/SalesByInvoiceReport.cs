using Inventory.Models;
using Inventory.Printing;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 3 - Sales by invoice: every sale and return invoice of the period with its total, cost and profit,
    // and the total sales and profit at the top. Returns are negative (red), so the TOTAL row is the net.
    // Double-click an invoice: its receipt (items, discounts, payments).
    public sealed class SalesByInvoiceReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Sales by invoice";

        public override string Description =>
            "Every sale and return invoice with its total, cost and profit. Returns are negative, so the total is the net.";

        public override (DateTime From, DateTime To) DefaultPeriod => (DateTime.Today, DateTime.Today);

        public override string? DoubleClickHint => "Double-click an invoice to see its receipt.";

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var invoices = await new ReportRepository().GetSalesByInvoiceAsync(filter.From, filter.To, filter.UserId);
            var total = SalesByInvoiceRow.TotalOf(invoices);
            var rows = new List<SalesByInvoiceRow>(invoices) { total };

            var sales = invoices.Where(i => !i.IsReturn).ToList();
            var returns = invoices.Where(i => i.IsReturn).ToList();
            var biggest = sales.MaxBy(i => i.Total);
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
                    new("INVOICES", $"{sales.Count:#,0}", Dark,
                        sales.Count > 0 ? $"average {money.FormatPrimary(sales.Average(i => i.Total))}" : null),
                    new("RETURNS", money.FormatPrimary(-returns.Sum(i => i.Total)), returns.Count > 0 ? Red : Dark, $"{returns.Count:#,0} return invoice(s)"),
                    new("DISCOUNTS", money.FormatPrimary(sales.Sum(i => i.Discounts)), Orange,
                        sales.Sum(i => i.Gross) is > 0 and var gross ? $"{sales.Sum(i => i.Discounts) / gross * 100:0.#}% of gross" : null),
                    new("BIGGEST INVOICE", biggest != null ? money.FormatPrimary(biggest.Total) : "-", Green, biggest?.InvoiceNo),
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
                if (type != typeof(decimal) && type != typeof(long)) continue;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.Format = column.DataPropertyName switch
                {
                    nameof(SalesByInvoiceRow.Margin) => "0.0",
                    nameof(SalesByInvoiceRow.Quantity) => "#,0.###",
                    _ => "#,0",
                };
            }
            if (grid.Columns[nameof(SalesByInvoiceRow.InvoiceNo)] is { } no) no.FillWeight = 120;
            if (grid.Columns[nameof(SalesByInvoiceRow.InvoiceDate)] is { } date) date.FillWeight = 115;
            if (grid.Columns[nameof(SalesByInvoiceRow.Payments)] is { } paid) paid.FillWeight = 130;
            foreach (var narrow in new[] { nameof(SalesByInvoiceRow.TypeText), nameof(SalesByInvoiceRow.ItemCount),
                                           nameof(SalesByInvoiceRow.Quantity), nameof(SalesByInvoiceRow.Margin) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 55;
            foreach (var bold in new[] { nameof(SalesByInvoiceRow.Total), nameof(SalesByInvoiceRow.Profit) })
                if (grid.Columns[bold] is { } c) c.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // Returns in red, a loss in red, discounts in orange, the TOTAL row bold on a light background
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not SalesByInvoiceRow invoice) return;
            if (invoice.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
            }
            else if (invoice.IsReturn)
            {
                style.ForeColor = style.SelectionForeColor = Red;
            }
            if (column.DataPropertyName is nameof(SalesByInvoiceRow.Profit) or nameof(SalesByInvoiceRow.Margin) && invoice.Profit < 0 && !invoice.IsReturn)
                style.ForeColor = style.SelectionForeColor = Red;
            if (column.DataPropertyName == nameof(SalesByInvoiceRow.Discounts) && invoice.Discounts > 0 && !invoice.IsTotal)
                style.ForeColor = style.SelectionForeColor = Orange;
        }

        public override bool IsTotalRow(object row) => row is SalesByInvoiceRow { IsTotal: true };

        // The receipt of that invoice (items, discounts, how it was paid); can be printed from Sales (F7)
        public override async void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter)
        {
            if (row is not SalesByInvoiceRow { IsTotal: false } invoice) return;
            try
            {
                var settings = ReceiptSettings.Current ?? await ReceiptSettings.LoadAsync();
                if (await new SaleRepository().GetReceiptAsync(invoice.SaleId) is { } receipt)
                    ReceiptPrinter.ShowPreview(owner, receipt, filter.Currencies, settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Could not show the invoice:\n" + ex.Message, Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
