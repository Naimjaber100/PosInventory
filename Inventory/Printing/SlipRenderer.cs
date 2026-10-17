using Inventory.Models;

namespace Inventory.Printing
{
    // A short report for the 80 mm roll (Reports > Print 80 mm), e.g. Sales by item:
    //
    //            My Store
    //         SALES BY ITEM
    //    01/10/2026 - 16/10/2026
    //   ------------------------------
    //   Pepsi
    //      12 pcs x 45,000     540,000
    //      Cost 400,000   Profit 140,000
    //   ...
    //   ------------------------------
    //   NET SALES            2,450,000
    //   PROFIT                 610,000
    public sealed class ReportSlip
    {
        public string Company { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public List<string> Header { get; init; } = new();   // period, filters (small, centred)
        public string? ColumnsLeft { get; init; }             // what the two columns of a row are, e.g. "Qty x avg price"
        public string? ColumnsRight { get; init; }
        public List<SlipRow> Rows { get; init; } = new();
        public List<SlipTotal> Totals { get; init; } = new();
    }

    /// <summary>One row: the name on its own line(s), then one or two lines of left / right text.</summary>
    public sealed record SlipRow(string Name, string Left, string Right, string? Left2 = null, string? Right2 = null);

    /// <summary>A total under the rows; Strong = big and bold (the main result).</summary>
    public sealed record SlipTotal(string Caption, string Value, bool Strong = false);

    public sealed class SlipRenderer : RollRenderer
    {
        private readonly ReportSlip _slip;
        private readonly DateTime _printedAt = DateTime.Now;

        public SlipRenderer(ReportSlip slip, CurrencySet currencies, int paperWidthMm) : base(currencies, paperWidthMm)
        {
            _slip = slip;
        }

        public override string DocumentName => _slip.Title;

        public override float Render(Graphics? g)
        {
            var y = 0F;
            if (_slip.Company.Length > 0) y = Text(g, _slip.Company, Title, y, Align.Center);
            y = Text(g, _slip.Title.ToUpperInvariant(), Bold, y + 2, Align.Center);
            foreach (var line in _slip.Header) y = Text(g, line, Small, y, Align.Center);
            y = Separator(g, y + 4);

            if (_slip.ColumnsLeft != null || _slip.ColumnsRight != null)
            {
                y = TwoColumns(g, _slip.ColumnsLeft ?? string.Empty, _slip.ColumnsRight ?? string.Empty, Small, y);
                y = Separator(g, y);
            }

            foreach (var row in _slip.Rows)
            {
                y = Text(g, row.Name, Bold, y, Align.Auto);
                y = TwoColumns(g, row.Left, row.Right, Normal, y);
                if (row.Left2 != null || row.Right2 != null) y = TwoColumns(g, row.Left2 ?? string.Empty, row.Right2 ?? string.Empty, Small, y);
                y += 4;
            }
            if (_slip.Rows.Count == 0) y = Text(g, "Nothing in this period.", Normal, y, Align.Center);
            y = Separator(g, y);

            foreach (var total in _slip.Totals)
                y = TwoColumns(g, total.Caption, total.Value, total.Strong ? Big : Normal, y + (total.Strong ? 2 : 0));

            y = Separator(g, y + 4);
            y = Text(g, $"Printed {_printedAt:dd/MM/yyyy HH:mm}  ·  {AppSession.UserName}", Small, y, Align.Center);
            return y + 20;   // a little paper after the text, before the cut
        }
    }
}
