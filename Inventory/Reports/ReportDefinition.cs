using System.Collections;
using Inventory.Models;

namespace Inventory.Reports
{
    // One report for the shared report screen (ReportForm): what it is called, how its rows are loaded,
    // the total cards at the top, how its cells look and what double-click on a row does.
    // A new report = a database function + a class like DailySalesReport.
    public abstract class ReportDefinition
    {
        public abstract string Title { get; }

        /// <summary>One line under the title: what the report shows.</summary>
        public abstract string Description { get; }

        /// <summary>Shows the Cashier filter (All users or one cashier).</summary>
        public virtual bool UsesCashierFilter => true;

        /// <summary>The period the report opens with (This month by default).</summary>
        public virtual (DateTime From, DateTime To) DefaultPeriod => (new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today);

        public abstract Task<ReportResult> LoadAsync(ReportFilter filter);

        /// <summary>Column widths, formats and alignment after the rows are shown.</summary>
        public virtual void FormatGrid(DataGridView grid) { }

        /// <summary>Colors of one cell (e.g. a loss in red, the total row in bold).</summary>
        public virtual void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style) { }

        /// <summary>The total row is bold in the grid and in Excel.</summary>
        public virtual bool IsTotalRow(object row) => false;

        /// <summary>Shown under the grid when double-click does something.</summary>
        public virtual string? DoubleClickHint => null;

        public virtual void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter) { }
    }

    /// <summary>The filters chosen on the report screen.</summary>
    public sealed class ReportFilter
    {
        public DateTime From { get; init; }
        public DateTime To { get; init; }
        public int? UserId { get; init; }                 // null = every cashier
        public string UserName { get; init; } = "All users";
        public CurrencySet Currencies { get; init; } = CurrencySet.Default;

        public string PeriodText => From.Date == To.Date ? From.ToString("ddd dd/MM/yyyy") : $"{From:dd/MM/yyyy} - {To:dd/MM/yyyy}";
    }

    /// <summary>The rows of a report and its total cards.</summary>
    public sealed class ReportResult
    {
        public IList Rows { get; init; } = Array.Empty<object>();
        public List<ReportCard> Cards { get; init; } = new();
    }

    /// <summary>A total at the top of the report: caption, value, color and a small line under it.</summary>
    public sealed record ReportCard(string Caption, string Value, Color Color, string? Note = null);
}
