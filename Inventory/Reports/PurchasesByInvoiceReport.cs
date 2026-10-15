using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 7 - Purchases by invoice: every posted purchase of the period with its supplier, total, what was paid
    // and what we still owe for it. Filter: one supplier or all. Drafts are left out.
    // Double-click a purchase: the purchase itself (read-only, as in Purchase).
    public sealed class PurchasesByInvoiceReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Purchases by invoice";

        public override string Description =>
            "Every posted purchase with its supplier, total, what was paid and what is still owed. Drafts are not counted.";

        public override bool UsesCashierFilter => false;

        public override bool UsesSupplierFilter => true;

        public override string? DoubleClickHint => "Double-click a purchase to open it.";

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var invoices = await new ReportRepository().GetPurchasesByInvoiceAsync(filter.From, filter.To, filter.SupplierId);
            var total = PurchasesByInvoiceRow.TotalOf(invoices);
            var rows = new List<PurchasesByInvoiceRow>(invoices) { total };

            var money = filter.Currencies;
            var biggest = invoices.MaxBy(i => i.TotalLbp);
            var unpaid = invoices.Where(i => i.OwedLbp > 0).ToList();
            var suppliers = invoices.Select(i => i.Supplier ?? string.Empty).Distinct().Count();
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("TOTAL PURCHASES", money.FormatPrimary(total.TotalLbp), Blue,
                        money.FormatSecond(total.TotalLbp) is { Length: > 0 } s ? s : null),
                    new("PURCHASES", $"{invoices.Count:#,0}", Dark,
                        invoices.Count > 0 ? $"average {money.FormatPrimary(invoices.Average(i => i.TotalLbp))}  ·  {suppliers} supplier(s)" : null),
                    new("PAID", money.FormatPrimary(total.PaidLbp), Green,
                        total.TotalLbp > 0 ? $"{total.PaidLbp / total.TotalLbp * 100:0.#}% of the purchases" : null),
                    new("STILL OWED", money.FormatPrimary(total.OwedLbp), total.OwedLbp > 0 ? Orange : Dark, $"{unpaid.Count:#,0} purchase(s) not fully paid"),
                    new("DISCOUNTS", money.FormatPrimary(total.DiscountLbp), Green, "given by the suppliers"),
                    new("BIGGEST PURCHASE", biggest != null ? money.FormatPrimary(biggest.TotalLbp) : "-", Blue,
                        biggest != null ? $"{biggest.PurchaseNo}  ·  {biggest.Supplier ?? "no supplier"}" : null),
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
                    column.DefaultCellStyle.Format = "dd/MM/yyyy";
                    continue;
                }
                if (type != typeof(decimal) && type != typeof(long)) continue;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.Format = "#,0";
            }
            if (grid.Columns[nameof(PurchasesByInvoiceRow.TotalText)] is { } own) own.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (grid.Columns[nameof(PurchasesByInvoiceRow.PurchaseNo)] is { } no) no.FillWeight = 120;
            if (grid.Columns[nameof(PurchasesByInvoiceRow.Supplier)] is { } supplier) supplier.FillWeight = 120;
            foreach (var narrow in new[] { nameof(PurchasesByInvoiceRow.ItemCount), nameof(PurchasesByInvoiceRow.Status) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 60;
            if (grid.Columns[nameof(PurchasesByInvoiceRow.InvoiceDate)] is { } date) date.FillWeight = 85;
            foreach (var bold in new[] { nameof(PurchasesByInvoiceRow.TotalLbp), nameof(PurchasesByInvoiceRow.OwedLbp) })
                if (grid.Columns[bold] is { } c) c.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // Still owed in orange, paid in green, the TOTAL row bold on a light background
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not PurchasesByInvoiceRow invoice) return;
            if (invoice.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
                return;
            }
            if (column.DataPropertyName is nameof(PurchasesByInvoiceRow.OwedLbp) or nameof(PurchasesByInvoiceRow.Status))
                style.ForeColor = style.SelectionForeColor = invoice.OwedLbp > 0 ? Orange : Green;
            if (column.DataPropertyName == nameof(PurchasesByInvoiceRow.Supplier) && invoice.Supplier == null)
            {
                style.ForeColor = style.SelectionForeColor = Color.Gray;
            }
        }

        public override bool IsTotalRow(object row) => row is PurchasesByInvoiceRow { IsTotal: true };

        // The purchase, read-only (a posted purchase cannot be changed)
        public override void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter)
        {
            if (row is not PurchasesByInvoiceRow { IsTotal: false } invoice) return;
            using var form = new PurchaseEditForm(invoice.PurchaseId);
            form.ShowDialog(owner);
        }
    }
}
