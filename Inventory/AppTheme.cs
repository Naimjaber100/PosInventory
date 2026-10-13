namespace Inventory
{
    // One look for every back-office window: the same background, header, buttons and lists as the dashboard.
    // A form calls AppTheme.Apply(this) after InitializeComponent(); its controls are restyled when it loads:
    //   - window background: light grey; cards and bars stay white
    //   - a colored header bar: dark (as the menu on the left)
    //   - main buttons (Save, Add..., any colored button): the accent green; Delete-like red buttons: red;
    //     every other button: white with a thin border (red text stays red)
    //   - lists: grey column names, thin lines between rows, a light green selected row
    // The cashier and the login screen keep their own touch-screen design (same colors, bigger keys).
    public static class AppTheme
    {
        public static readonly Color Background = Color.FromArgb(243, 245, 248);
        public static readonly Color Dark = Color.FromArgb(22, 32, 43);
        public static readonly Color Accent = Color.FromArgb(34, 139, 117);
        public static readonly Color AccentHover = Color.FromArgb(28, 118, 99);
        public static readonly Color Danger = Color.FromArgb(192, 57, 43);
        public static readonly Color Border = Color.FromArgb(206, 212, 220);
        public static readonly Color HeaderBack = Color.FromArgb(240, 242, 245);
        public static readonly Color HeaderText = Color.FromArgb(70, 80, 92);
        public static readonly Color Selection = Color.FromArgb(220, 240, 234);
        public static readonly Color GridLine = Color.FromArgb(232, 235, 240);

        /// <summary>
        /// Gives the form the common look: at once (so it never shows in its old colors first), and again when it
        /// loads, for the controls it builds in code after InitializeComponent().
        /// </summary>
        public static void Apply(Form form)
        {
            StyleForm(form);
            form.Load += (_, _) => StyleForm(form);
        }

        private static void StyleForm(Form form)
        {
            form.SuspendLayout();
            form.BackColor = Background;
            Style(form, form.AcceptButton as Button);
            form.ResumeLayout();
        }

        private static void Style(Control parent, Button? accept)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case Button button:
                        StyleButton(button, ReferenceEquals(button, accept));
                        break;
                    case DataGridView grid:
                        StyleGrid(grid);
                        break;
                    default:
                        if (!IsPlainContainer(control)) break;   // our own controls (cards, color pickers...) draw themselves
                        if (IsColored(control.BackColor)) control.BackColor = Dark;                 // a header bar
                        else if (IsLightGrey(control.BackColor)) control.BackColor = Background;   // an older grey
                        Style(control, accept);
                        break;
                }
            }
        }

        // Panels, tables, group boxes, tab pages...: the containers of Windows Forms itself
        private static bool IsPlainContainer(Control control) =>
            control.HasChildren && control.GetType().Namespace == typeof(Panel).Namespace && control is not DataGridView;

        private static void StyleButton(Button button, bool isAccept)
        {
            // The stock filter chips of the item list are colored by what they mean, in code
            if (button.Name.EndsWith("Chip", StringComparison.Ordinal)) return;

            var back = button.BackColor;
            if (IsDark(back)) return;   // a dark button on a dark bar stays as it is

            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            if (IsColored(back) || isAccept)
            {
                var color = IsRed(back) ? Danger : Accent;
                button.BackColor = color;
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = color == Danger ? Color.FromArgb(166, 48, 36) : AccentHover;
            }
            else
            {
                button.BackColor = Color.White;
                if (button.ForeColor == SystemColors.ControlText) button.ForeColor = Dark;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.MouseOverBackColor = HeaderBack;
            }
        }

        private static void StyleGrid(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = GridLine;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            var header = grid.ColumnHeadersDefaultCellStyle;
            header.BackColor = header.SelectionBackColor = HeaderBack;
            header.ForeColor = header.SelectionForeColor = HeaderText;
            header.Font = new Font(grid.Font.FontFamily, grid.Font.Size, FontStyle.Bold);
            if (grid.ColumnHeadersHeightSizeMode != DataGridViewColumnHeadersHeightSizeMode.AutoSize && grid.ColumnHeadersHeight < 34)
                grid.ColumnHeadersHeight = 34;

            grid.DefaultCellStyle.SelectionBackColor = Selection;
            grid.DefaultCellStyle.SelectionForeColor = Dark;
            grid.RowHeadersDefaultCellStyle.BackColor = HeaderBack;
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = Selection;
        }

        // A real color (blue, green, purple, red...), not white, grey or the dark of the bars
        private static bool IsColored(Color c) =>
            c.A == 255 && !c.IsSystemColor && c.GetSaturation() > 0.3f && c.GetBrightness() is > 0.2f and < 0.8f;

        private static bool IsDark(Color c) => c.A == 255 && !c.IsSystemColor && c.GetBrightness() <= 0.3f && !IsColored(c);

        private static bool IsRed(Color c) => c.GetHue() is < 20f or > 340f;

        private static bool IsLightGrey(Color c) =>
            c.A == 255 && !c.IsSystemColor && c.GetBrightness() is > 0.88f and < 0.99f && c.GetSaturation() < 0.35f && c.ToArgb() != Color.White.ToArgb();
    }
}
