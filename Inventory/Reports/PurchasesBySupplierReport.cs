using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 9 - Purchases by supplier: per supplier what was bought from them and paid to them in the period,
    // and what we owe them now (for all time). Purchases posted without a supplier are one "(no supplier)" row.
    // Double-click a supplier: the Purchases by invoice report for that supplier and period.
    public sealed class PurchasesBySupplierReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Purchases by supplier";

        public override string Description =>
            "What was bought from and paid to each supplier in the period, biggest first, and what we owe each one now.";

        public override bool UsesCashierFilter => false;

        public override string? DoubleClickHint => "Double-click a supplier to see their purchases.";

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var suppliers = await new ReportRepository().GetPurchasesBySupplierAsync(filter.From, filter.To);
            var total = PurchasesBySupplierRow.TotalOf(suppliers);
            foreach (var supplier in suppliers)
                supplier.Share = total.Purchases > 0 ? Math.Round(supplier.Purchases / total.Purchases * 100, 1) : null;
            var rows = new List<PurchasesBySupplierRow>(suppliers) { total };

            var money = filter.Currencies;
            var top = suppliers.FirstOrDefault(s => s.Purchases > 0);
            var mostOwed = suppliers.Where(s => s.OwedNow > 0).MaxBy(s => s.OwedNow);
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("TOTAL PURCHASES", money.FormatPrimary(total.Purchases), Blue,
                        money.FormatSecond(total.Purchases) is { Length: > 0 } s ? s : null),
                    new("SUPPLIERS", $"{suppliers.Count(s => s.Purchases > 0):#,0}", Dark, $"{total.InvoiceCount:#,0} purchase(s)"),
                    new("PAID IN PERIOD", money.FormatPrimary(total.Paid), Green, null),
                    new("WE OWE NOW", money.FormatPrimary(total.OwedNow ?? 0), total.OwedNow > 0 ? Orange : Dark, "to all these suppliers, for all time"),
                    new("TOP SUPPLIER", top?.Supplier ?? "-", Blue,
                        top != null ? $"{money.FormatPrimary(top.Purchases)}  ·  {top.Share:0.#}% of purchases" : null),
                    new("MOST OWED", mostOwed?.Supplier ?? "-", Orange,
                        mostOwed != null ? money.FormatPrimary(mostOwed.OwedNow ?? 0) : "nothing owed"),
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
                column.DefaultCellStyle.Format = column.DataPropertyName == nameof(PurchasesBySupplierRow.Share) ? "0.0" : "#,0";
            }
            if (grid.Columns[nameof(PurchasesBySupplierRow.Supplier)] is { } name) name.FillWeight = 150;
            foreach (var narrow in new[] { nameof(PurchasesBySupplierRow.InvoiceCount), nameof(PurchasesBySupplierRow.Share) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 60;
            foreach (var bold in new[] { nameof(PurchasesBySupplierRow.Purchases), nameof(PurchasesBySupplierRow.OwedNow) })
                if (grid.Columns[bold] is { } c) c.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // What we owe in orange, "(no supplier)" in grey, the TOTAL row bold on a light background
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not PurchasesBySupplierRow supplier) return;
            if (supplier.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
                return;
            }
            if (supplier.SupplierId == null) style.ForeColor = style.SelectionForeColor = Color.Gray;
            else if (column.DataPropertyName == nameof(PurchasesBySupplierRow.OwedNow) && supplier.OwedNow > 0)
                style.ForeColor = style.SelectionForeColor = Orange;
        }

        public override bool IsTotalRow(object row) => row is PurchasesBySupplierRow { IsTotal: true };

        // That supplier's purchases of the same period
        public override void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter)
        {
            if (row is not PurchasesBySupplierRow { IsTotal: false, SupplierId: int supplierId } supplier) return;
            var report = new SupplierPurchases(supplierId, supplier.Supplier, (filter.From, filter.To));
            using var form = new ReportForm(report) { StartPosition = FormStartPosition.CenterParent, Size = new Size(1300, 760) };
            form.ShowDialog(owner);
        }

        // Purchases by invoice for one supplier (no supplier box: it is fixed), opening on the same period
        private sealed class SupplierPurchases : ReportDefinition
        {
            private readonly PurchasesByInvoiceReport _invoices = new();
            private readonly int _supplierId;
            private readonly string _name;
            private readonly (DateTime From, DateTime To) _period;

            public SupplierPurchases(int supplierId, string name, (DateTime From, DateTime To) period) =>
                (_supplierId, _name, _period) = (supplierId, name, period);

            public override (DateTime From, DateTime To) DefaultPeriod => _period;
            public override string Title => $"Purchases from {_name}";
            public override string Description => _invoices.Description;
            public override bool UsesCashierFilter => false;
            public override string? DoubleClickHint => _invoices.DoubleClickHint;

            public override Task<ReportResult> LoadAsync(ReportFilter filter) =>
                _invoices.LoadAsync(new ReportFilter { From = filter.From, To = filter.To, SupplierId = _supplierId, SupplierName = _name, Currencies = filter.Currencies });

            public override void FormatGrid(DataGridView grid) => _invoices.FormatGrid(grid);
            public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style) => _invoices.FormatCell(row, column, style);
            public override bool IsTotalRow(object row) => _invoices.IsTotalRow(row);
            public override void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter) => _invoices.OnRowDoubleClick(owner, row, filter);
        }
    }
}
