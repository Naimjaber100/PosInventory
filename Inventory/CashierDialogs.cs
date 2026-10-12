using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Small pop-ups of the cashier screen, built in code
    public static class CashierDialogs
    {
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);

        /// <summary>
        /// Asks for a Manager's password ("why" says what needs approval).
        /// Returns that Manager, or null when cancelled or the password is not a Manager's.
        /// </summary>
        // Touch screen: a number keypad like the login screen (the keyboard works too)
        public static async Task<UserModel?> AskManagerApprovalAsync(IWin32Window owner, string why)
        {
            using var dialog = NewDialog("Manager approval", new Size(400, 600));
            dialog.BackColor = Color.White;

            var header = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Dark, Padding = new Padding(20, 12, 20, 8) };
            header.Controls.Add(new Label
            {
                Text = why,
                Dock = DockStyle.Fill,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 10.5F),
            });
            header.Controls.Add(new Label
            {
                Text = "Manager password",
                Dock = DockStyle.Top,
                Height = 32,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            });

            var password = new TextBox
            {
                UseSystemPasswordChar = true,
                Location = new Point(24, 100),
                Width = 352,
                Font = new Font("Segoe UI", 22F),
                TextAlign = HorizontalAlignment.Center,
            };

            var keypad = new TableLayoutPanel { Location = new Point(20, 168), Size = new Size(360, 320), ColumnCount = 3, RowCount = 4 };
            for (var i = 0; i < 3; i++) keypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            for (var i = 0; i < 4; i++) keypad.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "⌫" };
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var button = new Button
                {
                    Text = key,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(244, 246, 248),
                    ForeColor = key == "C" ? Color.Firebrick : Dark,
                    TabStop = false,
                    UseVisualStyleBackColor = false,
                };
                button.FlatAppearance.BorderColor = Color.Silver;
                button.Click += (_, _) =>
                {
                    switch (key)
                    {
                        case "C": password.Clear(); break;
                        case "⌫": if (password.TextLength > 0) password.Text = password.Text[..^1]; break;
                        default: password.AppendText(key); break;
                    }
                    password.Focus();
                    password.SelectionStart = password.TextLength;
                };
                keypad.Controls.Add(button, i % 3, i / 3);
            }

            var ok = new Button
            {
                Text = "Approve",
                DialogResult = DialogResult.OK,
                Location = new Point(24, 504),
                Size = new Size(218, 64),
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(34, 139, 117),
                ForeColor = Color.White,
                TabStop = false,
                UseVisualStyleBackColor = false,
            };
            ok.FlatAppearance.BorderSize = 0;
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(250, 504),
                Size = new Size(126, 64),
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 246, 248),
                ForeColor = Dark,
                TabStop = false,
                UseVisualStyleBackColor = false,
            };
            cancel.FlatAppearance.BorderColor = Color.Silver;

            dialog.Controls.AddRange(new Control[] { password, keypad, ok, cancel, header });
            dialog.AcceptButton = ok;      // Enter
            dialog.CancelButton = cancel;  // Esc
            dialog.Shown += (_, _) => password.Focus();

            if (dialog.ShowDialog(owner) != DialogResult.OK || password.TextLength == 0) return null;
            try
            {
                var user = await new UserRepository().FindByPasswordAsync(password.Text);
                if (user?.Role == UserModel.ManagerRole) return user;
                MessageBox.Show(owner, "That is not a manager password.", "Manager approval", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Could not check the password:\n" + ex.Message, "Manager approval", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }

        /// <summary>
        /// How the money of a return goes back: cash in any currency that has a rate, or card (primary currency).
        /// Returns (method, currency) or null when cancelled.
        /// </summary>
        public static (short Method, string Currency)? ChooseRefund(IWin32Window owner, decimal total, CurrencySet currencies)
        {
            var options = currencies.Payable
                .Select(c => (Text: $"Cash {c.Code}\n{c.Format(currencies.FromPrimary(total, c))}", Method: PaymentMethod.Cash, Currency: c.Code))
                .Append((Text: $"Card\n{currencies.FormatPrimary(total)}", Method: PaymentMethod.Card, Currency: currencies.Primary.Code))
                .ToList();
            var width = Math.Max(520, 20 + options.Count * 160);

            using var dialog = NewDialog("Refund", new Size(width, 250));
            dialog.BackColor = Dark;
            var caption = new Label
            {
                Text = "Give back to the customer",
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 11F),
                Location = new Point(20, 14),
                Size = new Size(width - 40, 26),
            };
            var second = currencies.FormatSecond(total);
            var totalLabel = new Label
            {
                Text = currencies.FormatPrimary(total) + (second.Length > 0 ? $"    ({second})" : string.Empty),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                Location = new Point(18, 42),
                Size = new Size(width - 30, 52),
            };
            dialog.Controls.AddRange(new Control[] { caption, totalLabel });

            (short, string)? choice = null;
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var button = new Button
                {
                    Text = option.Text,
                    Location = new Point(20 + i * 160, 112),
                    Size = new Size(150, 64),
                    Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = i == 0 ? Color.FromArgb(192, 57, 43) : Color.FromArgb(52, 64, 78),
                    ForeColor = Color.White,
                    UseVisualStyleBackColor = false,
                };
                button.FlatAppearance.BorderSize = 0;
                button.Click += (_, _) => { choice = (option.Method, option.Currency); dialog.DialogResult = DialogResult.OK; };
                dialog.Controls.Add(button);
                if (i == 0) dialog.AcceptButton = button;   // Enter = cash in the primary currency
            }
            var cancel = new Button
            {
                Text = "Back (Esc)",
                DialogResult = DialogResult.Cancel,
                Location = new Point(width - 130, 190),
                Size = new Size(110, 40),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Gainsboro,
                BackColor = Dark,
                UseVisualStyleBackColor = false,
            };
            cancel.FlatAppearance.BorderColor = Color.Gray;
            dialog.Controls.Add(cancel);
            dialog.CancelButton = cancel;

            return dialog.ShowDialog(owner) == DialogResult.OK ? choice : null;
        }

        /// <summary>
        /// Who takes an unpaid (on account) sale: an existing client from the list, or a new one (name and phone required).
        /// Returns the client (CustomerId 0 = new, not saved yet), or null when cancelled.
        /// </summary>
        public static async Task<CustomerModel?> ChooseCustomerAsync(IWin32Window owner, decimal total, CurrencySet currencies)
        {
            const int WalkInId = 1;   // the walk-in customer: never on account
            List<CustomerModel> all;
            try
            {
                all = (await new CustomerRepository().GetAllAsync()).Where(c => c.CustomerId != WalkInId).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Could not load the customers:\n" + ex.Message, "Unpaid sale", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            using var dialog = NewDialog("Unpaid sale", new Size(520, 590));
            dialog.BackColor = Color.White;

            var header = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Dark, Padding = new Padding(20, 12, 20, 8) };
            header.Controls.Add(new Label
            {
                Text = "Choose the customer, or type the name and phone of a new one.",
                Dock = DockStyle.Fill,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 10.5F),
            });
            header.Controls.Add(new Label
            {
                Text = $"Unpaid  ·  {currencies.FormatPrimary(total)}",
                Dock = DockStyle.Top,
                Height = 32,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            });

            var captionFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            var boxFont = new Font("Segoe UI", 14F);
            var phoneCaption = new Label { Text = "Phone", Location = new Point(24, 98), AutoSize = true, Font = captionFont, ForeColor = Color.DimGray };
            var phone = new TextBox { Location = new Point(24, 120), Width = 200, Font = boxFont, MaxLength = 30 };
            var nameCaption = new Label { Text = "Name", Location = new Point(236, 98), AutoSize = true, Font = captionFont, ForeColor = Color.DimGray };
            var name = new TextBox { Location = new Point(236, 120), Width = 260, Font = boxFont, MaxLength = 150 };
            var listCaption = new Label { Location = new Point(24, 166), Size = new Size(472, 22), Font = captionFont, ForeColor = Color.DimGray };
            var list = new ListBox
            {
                Location = new Point(24, 190),
                Size = new Size(472, 300),
                Font = new Font("Segoe UI", 11.5F),
                IntegralHeight = false,
                ItemHeight = 30,
                DrawMode = DrawMode.OwnerDrawFixed,
            };
            list.DrawItem += (_, e) =>
            {
                if (e.Index < 0) return;
                var c = (CustomerModel)list.Items[e.Index];
                e.DrawBackground();
                var color = (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : Dark;
                var owes = c.BalanceLbp > 0 ? $"owes {currencies.FormatPrimary(c.BalanceLbp)}" : string.Empty;
                var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
                TextRenderer.DrawText(e.Graphics, $"{c.Name}   ·   {c.Phone}", e.Font, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 190, e.Bounds.Height), color, flags);
                TextRenderer.DrawText(e.Graphics, owes, e.Font, new Rectangle(e.Bounds.Right - 184, e.Bounds.Y, 178, e.Bounds.Height), color, flags | TextFormatFlags.Right);
            };

            var ok = new Button
            {
                Text = "Continue",
                Location = new Point(24, 506),
                Size = new Size(338, 64),
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(34, 139, 117),
                ForeColor = Color.White,
                UseVisualStyleBackColor = false,
            };
            ok.FlatAppearance.BorderSize = 0;
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(370, 506),
                Size = new Size(126, 64),
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 246, 248),
                ForeColor = Dark,
                UseVisualStyleBackColor = false,
            };
            cancel.FlatAppearance.BorderColor = Color.Silver;

            static string Digits(string? text) => new((text ?? string.Empty).Where(char.IsDigit).ToArray());

            CustomerModel? chosen = null;   // the existing client picked from the list
            CustomerModel? result = null;
            var filling = false;

            // The list follows what is typed: every client whose phone and name contain it
            void Filter()
            {
                var digits = Digits(phone.Text);
                var words = name.Text.Trim();
                var matches = all.Where(c => (digits.Length == 0 || Digits(c.Phone).Contains(digits))
                                             && (words.Length == 0 || c.Name.Contains(words, StringComparison.CurrentCultureIgnoreCase)))
                    .Take(100).ToArray();
                list.BeginUpdate();
                list.Items.Clear();
                list.Items.AddRange(matches);
                list.EndUpdate();
                listCaption.Text = matches.Length > 0 ? "Existing customers (tap to choose)"
                    : all.Count == 0 ? "No customers yet: type the phone and the name."
                    : "Not found: a new customer is added with this phone and name.";
            }

            void Typed(object? sender, EventArgs e)
            {
                if (filling) return;
                chosen = null;
                Filter();
            }
            phone.TextChanged += Typed;
            name.TextChanged += Typed;

            list.SelectedIndexChanged += (_, _) =>
            {
                if (list.SelectedItem is not CustomerModel c) return;
                chosen = c;
                filling = true;
                phone.Text = c.Phone ?? string.Empty;
                name.Text = c.Name;
                filling = false;
            };

            ok.Click += (_, _) =>
            {
                if (chosen == null)
                {
                    var newName = name.Text.Trim();
                    var digits = Digits(phone.Text);
                    if (newName.Length == 0 || digits.Length < 6)
                    {
                        MessageBox.Show(dialog, "A new customer needs a name and a phone number.\nOr choose an existing customer from the list.",
                            "Unpaid sale", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        (digits.Length < 6 ? phone : name).Focus();
                        return;
                    }
                    // The same phone again is the same client, not a second one
                    chosen = all.FirstOrDefault(c => Digits(c.Phone) == digits);
                    if (chosen != null && MessageBox.Show(dialog, $"This phone is already the customer \"{chosen.Name}\".\nPut the sale on their account?",
                            "Unpaid sale", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        chosen = null;
                        return;
                    }
                    chosen ??= new CustomerModel { Name = newName, Phone = phone.Text.Trim() };
                }
                result = chosen;
                dialog.DialogResult = DialogResult.OK;
            };

            dialog.Controls.AddRange(new Control[] { phoneCaption, phone, nameCaption, name, listCaption, list, ok, cancel, header });
            dialog.AcceptButton = ok;      // Enter
            dialog.CancelButton = cancel;  // Esc
            dialog.Shown += (_, _) => phone.Focus();
            Filter();

            return dialog.ShowDialog(owner) == DialogResult.OK ? result : null;
        }

        private static Form NewDialog(string title, Size size) => new()
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = size,
            KeyPreview = true,
        };
    }
}
