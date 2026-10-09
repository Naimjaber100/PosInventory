using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Lists held (parked) sales: Recall brings one back to the cashier screen, Delete discards it
    public partial class HeldSalesForm : Form
    {
        private readonly SaleRepository _saleRepository = new();

        public HeldSalesForm()
        {
            InitializeComponent();
        }

        /// <summary>The held sale chosen with Recall (after DialogResult.OK).</summary>
        public HeldSaleModel? Selected { get; private set; }

        private async void HeldSalesForm_Load(object? sender, EventArgs e) => await LoadHeldAsync();

        private async Task LoadHeldAsync()
        {
            try
            {
                var held = (await _saleRepository.GetHeldSalesAsync()).ToList();
                heldGrid.DataSource = held;
                Text = $"Held sales ({held.Count})";
                recallButton.Enabled = deleteButton.Enabled = held.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load held sales:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Recall()
        {
            if (heldGrid.CurrentRow?.DataBoundItem is not HeldSaleModel sale) return;
            Selected = sale;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void recallButton_Click(object? sender, EventArgs e) => Recall();

        private void heldGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) Recall();
        }

        private void heldGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Recall();
        }

        private async void deleteButton_Click(object? sender, EventArgs e)
        {
            if (heldGrid.CurrentRow?.DataBoundItem is not HeldSaleModel sale) return;
            if (MessageBox.Show(this, $"Delete held sale #{sale.SaleId} ({sale.Total:#,0} LBP)?", "Delete held sale",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                await _saleRepository.DeleteHeldAsync(sale.SaleId);
                await LoadHeldAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete the held sale:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void heldGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (heldGrid.Columns[nameof(HeldSaleModel.HeldAt)] is { } at) at.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
            if (heldGrid.Columns[nameof(HeldSaleModel.Total)] is { } total)
            {
                total.DefaultCellStyle.Format = "#,0";
                total.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
    }
}
