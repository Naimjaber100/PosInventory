namespace Inventory
{
    // A row of color swatches (QuickButtonStyle.Palette) plus "More..." for any color.
    // The chosen swatch shows a thick border.
    public class ColorSwatchPicker : FlowLayoutPanel
    {
        private Color _color = QuickButtonStyle.DefaultColor;

        public ColorSwatchPicker()
        {
            WrapContents = false;
            Padding = new Padding(0, 4, 0, 0);

            foreach (var color in QuickButtonStyle.Palette)
            {
                var swatch = new Button
                {
                    Size = new Size(34, 34),
                    Margin = new Padding(0, 0, 6, 0),
                    BackColor = color,
                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                    Tag = color,
                };
                swatch.Click += (_, _) => SelectedColor = color;
                Controls.Add(swatch);
            }

            var more = new Button { Text = "More...", Size = new Size(80, 34), Margin = new Padding(0) };
            more.Click += (_, _) =>
            {
                using var dialog = new ColorDialog { Color = _color, FullOpen = true };
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) SelectedColor = dialog.Color;
            };
            Controls.Add(more);
            MarkSelected();
        }

        public event EventHandler? ColorChanged;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color SelectedColor
        {
            get => _color;
            set
            {
                _color = value;
                MarkSelected();
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void MarkSelected()
        {
            foreach (var swatch in Controls.OfType<Button>().Where(b => b.Tag is Color))
            {
                var selected = ((Color)swatch.Tag!).ToArgb() == _color.ToArgb();
                swatch.FlatAppearance.BorderSize = selected ? 3 : 1;
                swatch.FlatAppearance.BorderColor = selected ? Color.FromArgb(22, 32, 43) : Color.Silver;
            }
        }
    }
}
