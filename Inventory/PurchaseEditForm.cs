using System.ComponentModel;
using Inventory.Models;
using Inventory.Repositories;
using Inventory.Validation;

namespace Inventory
{
    // Add a purchase invoice (no id) or open an existing one (id).
    // Drafts can be changed; posted / cancelled invoices open read-only.
    public partial class PurchaseEditForm : Form
    {
        private readonly PurchaseRepository _purchaseRepository = new();
        private readonly BindingList<PurchaseEditLine> _lines = new();
        private readonly int? _purchaseId;
        private PurchaseHeaderModel _header = new();
        private bool _loading = true;   // true while filling the form, so it does not count as a change
        private bool _dirty;
        private readonly ErrorProvider _errors = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        public PurchaseEditForm(int? purchaseId = null)
        {
            InitializeComponent();
            _purchaseId = purchaseId;
            _errors.ContainerControl = this;

            _lines.ListChanged += (_, _) => { UpdateTotals(); MarkDirty(); };
            linesGrid.DataSource = _lines;
            linesGrid.CellValueChanged += (_, e) =>
            {
                // A corrected cell loses its error mark
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0) linesGrid[e.ColumnIndex, e.RowIndex].ErrorText = string.Empty;
            };
            dateInput.Value = DateTime.Today;
            UpdateTotals();
        }

        private void MarkDirty()
        {
            if (!_loading) _dirty = true;
        }

        private void headerField_Changed(object? sender, EventArgs e) => MarkDirty();

        private void PurchaseEditForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK || IsReadOnly || !_dirty) return;
            if (MessageBox.Show(this, "Close without saving your changes?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                e.Cancel = true;
        }

        /// <summary>The id of the saved purchase (after DialogResult.OK).</summary>
        public int SavedPurchaseId { get; private set; }

        private bool IsReadOnly => !_header.IsDraft;

        private async void PurchaseEditForm_Load(object? sender, EventArgs e)
        {
            if (_purchaseId == null)
            {
                Text = "New purchase";
                statusValue.Text = "New";
                ActiveControl = scanBox;
                _loading = false;
                return;
            }

            try
            {
                UseWaitCursor = true;
                var header = await _purchaseRepository.GetByIdAsync(_purchaseId.Value);
                if (header == null)
                {
                    MessageBox.Show(this, "This purchase no longer exists.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }
                _header = header;

                foreach (var l in await _purchaseRepository.GetLinesAsync(header.PurchaseId))
                {
                    _lines.Add(new PurchaseEditLine
                    {
                        PackagingId = l.PackagingId,
                        ProductId = l.ProductId,
                        ItemName = l.ItemName,
                        PackagingName = l.PackagingName,
                        Barcode = l.Barcode,
                        Factor = l.Factor,
                        BaseUnit = l.BaseUnit,
                        IsWeighted = l.IsWeighted,
                        Quantity = l.Quantity,
                        UnitCost = l.UnitCost,
                    });
                }
                FillHeader();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the purchase:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            finally
            {
                UseWaitCursor = false;
                _loading = false;
            }
        }

        private void FillHeader()
        {
            numberValue.Text = _header.PurchaseNo ?? "(given when posted)";
            supplierInvoiceText.Text = _header.SupplierInvoiceNo;
            dateInput.Value = _header.InvoiceDate;
            notesText.Text = _header.Notes;
            discountInput.Value = Math.Min(_header.Discount, discountInput.Maximum);
            statusValue.Text = _header.Status switch { 0 => "Draft", 1 => "Posted", _ => "Cancelled" };

            Text = _header.IsDraft
                ? $"Edit purchase (draft #{_header.PurchaseId})"
                : $"Purchase {_header.PurchaseNo} - {statusValue.Text} (read-only)";

            if (IsReadOnly)
            {
                supplierInvoiceText.ReadOnly = true;
                notesText.ReadOnly = true;
                dateInput.Enabled = false;
                discountInput.Enabled = false;
                scanPanel.Enabled = false;
                linesGrid.ReadOnly = true;
                linesGrid.AllowUserToDeleteRows = false;
                saveDraftButton.Visible = false;
                postButton.Visible = false;
                cancelButton.Text = "Close";
            }
            UpdateTotals();
        }

        #region Adding lines

        private async void scanBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await AddFromSearchAsync();
        }

        private async void addLineButton_Click(object? sender, EventArgs e)
        {
            await AddFromSearchAsync();
        }

        private async Task AddFromSearchAsync()
        {
            var search = scanBox.Text.Trim();
            if (search.Length == 0)
            {
                scanBox.Focus();
                return;
            }

            try
            {
                UseWaitCursor = true;
                var matches = (await _purchaseRepository.FindPackagingAsync(search)).ToList();
                UseWaitCursor = false;

                PackagingLookupModel? chosen;
                if (matches.Count == 0)
                {
                    MessageBox.Show(this, $"No item found for \"{search}\".\nOnly active standard items that can be purchased are listed.",
                        "Add item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    scanBox.SelectAll();
                    scanBox.Focus();
                    return;
                }
                else if (matches.Count == 1)
                {
                    chosen = matches[0];
                }
                else
                {
                    using var picker = new PackagingPickerForm(matches, search);
                    if (picker.ShowDialog(this) != DialogResult.OK) return;
                    chosen = picker.Selected;
                }

                if (chosen != null) AddLine(chosen);
                scanBox.Clear();
                scanBox.Focus();   // ready for the next scan
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not search items:\n" + ex.Message, "Add item",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        // Same packaging again adds 1 to its quantity; otherwise a new line with the last cost
        private void AddLine(PackagingLookupModel item)
        {
            var existing = _lines.FirstOrDefault(l => l.PackagingId == item.PackagingId);
            if (existing != null)
            {
                existing.Quantity += 1;
                SelectLine(existing);
                return;
            }

            var line = new PurchaseEditLine
            {
                PackagingId = item.PackagingId,
                ProductId = item.ProductId,
                ItemName = item.ItemName,
                PackagingName = item.PackagingName,
                Barcode = item.Barcode,
                Factor = item.Factor,
                BaseUnit = item.BaseUnit,
                IsWeighted = item.IsWeighted,
                Quantity = 1,
                UnitCost = item.LastCost,
            };
            _lines.Add(line);
            SelectLine(line);
        }

        private void SelectLine(PurchaseEditLine line)
        {
            var index = _lines.IndexOf(line);
            if (index < 0 || index >= linesGrid.Rows.Count) return;
            linesGrid.ClearSelection();
            linesGrid.Rows[index].Selected = true;
            linesGrid.FirstDisplayedScrollingRowIndex = index;
        }

        private void removeLineButton_Click(object? sender, EventArgs e)
        {
            if (IsReadOnly) return;
            if (linesGrid.CurrentRow?.DataBoundItem is PurchaseEditLine line) _lines.Remove(line);
        }

        #endregion

        #region Totals and grid

        private void discountInput_ValueChanged(object? sender, EventArgs e)
        {
            UpdateTotals();
            MarkDirty();
        }

        private void UpdateTotals()
        {
            var sub = _lines.Sum(l => l.LineTotal);
            subTotalValue.Text = sub.ToString("#,0.##");
            totalValue.Text = (sub - discountInput.Value).ToString("#,0.##");
            totalValue.ForeColor = sub - discountInput.Value < 0 ? Color.Firebrick : SystemColors.ControlText;
            itemsGroup.Text = $"Items ({_lines.Count} line(s), {_lines.Sum(l => l.Quantity):#,0.###} qty)";
        }

        private void linesGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewColumn column in linesGrid.Columns)
            {
                if (column.ValueType != typeof(decimal)) continue;
                column.DefaultCellStyle.Format = "#,0.###";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (!column.ReadOnly) column.DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 230);   // editable cells
            }
        }

        private void linesGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show(this, "Enter a valid number.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }

        #endregion

        #region Save

        private async void saveDraftButton_Click(object? sender, EventArgs e) => await SaveAsync(post: false);

        private async void postButton_Click(object? sender, EventArgs e) => await SaveAsync(post: true);

        private async Task SaveAsync(bool post)
        {
            if (!linesGrid.EndEdit()) return;   // apply a cell that is still being edited

            _header.SupplierInvoiceNo = supplierInvoiceText.Text;
            _header.InvoiceDate = dateInput.Value.Date;
            _header.Discount = discountInput.Value;
            _header.Notes = notesText.Text;

            // Check everything here first; the database only gets valid data (and checks again)
            var sub = _lines.Sum(l => l.LineTotal);
            var validation = PurchaseHeaderValidator.Validate(_header, sub, _lines.Count, post)
                .Merge(PurchaseLineValidator.ValidateAll(_lines));
            ShowValidation(validation);
            if (!validation.IsValid)
            {
                MessageBox.Show(this, "Please correct these first:" + Environment.NewLine + Environment.NewLine + validation.ToMessage(),
                    post ? "Post purchase" : "Save purchase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (post && MessageBox.Show(this,
                    $"Post this purchase ({sub - _header.Discount:#,0.##})?\n\n" +
                    "The items are added to stock and the average cost is updated.\nA posted purchase cannot be changed.",
                    "Post purchase", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                SetBusy(true);
                SavedPurchaseId = await _purchaseRepository.SaveAsync(_header, _lines, post, AppConfig.CurrentUserId);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                // Nothing is saved when anything fails (the database function is all or nothing)
                MessageBox.Show(this, "Could not save the purchase:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // Marks the wrong header fields (error icon) and grid cells (error icon + tooltip); clears old marks
        private void ShowValidation(ValidationResult validation)
        {
            var headerControls = new Dictionary<string, Control>
            {
                [nameof(PurchaseHeaderModel.SupplierInvoiceNo)] = supplierInvoiceText,
                [nameof(PurchaseHeaderModel.InvoiceDate)] = dateInput,
                [nameof(PurchaseHeaderModel.Discount)] = discountInput,
                [nameof(PurchaseHeaderModel.Notes)] = notesText,
            };
            foreach (var control in headerControls.Values) _errors.SetError(control, string.Empty);
            foreach (DataGridViewRow row in linesGrid.Rows)
                foreach (DataGridViewCell cell in row.Cells) cell.ErrorText = string.Empty;

            foreach (var error in validation.Errors)
            {
                if (error.LineIndex is int index && index < linesGrid.Rows.Count)
                {
                    var column = error.Field != null ? linesGrid.Columns[error.Field] : linesGrid.Columns[nameof(PurchaseEditLine.ItemName)];
                    if (column != null) linesGrid[column.Index, index].ErrorText = error.Message;
                }
                else if (error.Field != null && headerControls.TryGetValue(error.Field, out var control))
                {
                    _errors.SetError(control, error.Message);
                }
            }

            var firstBadLine = validation.Errors.FirstOrDefault(e => e.LineIndex != null)?.LineIndex;
            if (firstBadLine is int line && line < _lines.Count) SelectLine(_lines[line]);
        }

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            saveDraftButton.Enabled = !busy;
            postButton.Enabled = !busy;
        }

        #endregion

        private void cancelButton_Click(object sender, EventArgs e)
        {

        }
    }
}
