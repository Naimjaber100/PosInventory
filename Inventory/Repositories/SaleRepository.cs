using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Cashier screen: item lookup, saving, holding and recalling sales
    public class SaleRepository : BaseRepository
    {
        public SaleRepository(string? connectionString = null) : base(connectionString) { }

        /// <summary>Sellable packagings by exact barcode (one result), barcode start or name words.</summary>
        public async Task<IEnumerable<PosItemModel>> FindItemAsync(string search, int limit = 30)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_search", search);
            parameters.Add("p_limit", limit);

            return await GetListAsync<PosItemModel>(Procedures.PosFindItem, parameters);
        }

        /// <summary>The item of a scale label by its scale code (null when no scale product has it).</summary>
        public async Task<PosItemModel?> FindScaleItemAsync(string scaleCode)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_scale_code", scaleCode);

            return await GetSingleAsync<PosItemModel>(Procedures.PosFindScaleItem, parameters);
        }

        /// <summary>Everything the printed receipt of a sale or return needs (one call), or null.</summary>
        public async Task<Printing.Receipt?> GetReceiptAsync(int saleId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_sale_id", saleId);

            return Printing.Receipt.FromJson(await GetSingleAsync<string?>(Procedures.SaleReceipt, parameters));
        }

        /// <summary>A value from the settings table (e.g. "store_name"), or defaultValue.</summary>
        public async Task<string?> GetSettingAsync(string key, string? defaultValue = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_key", key);
            parameters.Add("p_default", defaultValue, DbType.String);

            return await GetSingleAsync<string?>(Procedures.GetSetting, parameters);
        }

        /// <summary>Adds or changes a value in the settings table.</summary>
        public async Task SetSettingAsync(string key, string value)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_key", key);
            parameters.Add("p_value", value);

            await ExecuteAsync(Procedures.SetSetting, parameters);
        }

        /// <summary>
        /// Saves and finishes a sale: stock out, payments, change. heldSaleId = the recalled held sale it replaces.
        /// customerId = the client of an on-account (unpaid) payment: that amount is added to what they owe.
        /// invoiceDiscount = the whole invoice discount; promoDiscount = the part of it an invoice promotion gave (promoName).
        /// The lines go with their whole discount, and what their promotions gave of it.
        /// All or nothing.
        /// </summary>
        public async Task<SaleResultModel> SaveSaleAsync(IEnumerable<PosLine> lines, IEnumerable<SalePaymentModel> payments,
            decimal invoiceDiscount, int? heldSaleId, int userId, int? customerId = null,
            decimal promoDiscount = 0, string? promoName = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", userId);
            parameters.Add("p_customer_id", customerId, DbType.Int32);
            parameters.Add("p_lines", new JsonbParameter(lines.Select(l => new
            {
                l.PackagingId, l.Quantity, l.UnitPrice, Discount = l.TotalDiscount, l.PromoDiscount, l.PromoName,
            }).ToList()));
            parameters.Add("p_payments", new JsonbParameter(payments.Where(p => p.Amount > 0)));
            parameters.Add("p_invoice_discount", invoiceDiscount);
            parameters.Add("p_held_sale_id", heldSaleId, DbType.Int32);
            parameters.Add("p_promo_discount", promoDiscount);
            parameters.Add("p_promo_name", promoName, DbType.String);

            return (await GetSingleAsync<SaleResultModel>(Procedures.SaveSale, parameters))!;
        }

        /// <summary>
        /// Parks the sale to finish later; replaceHeldId = the held sale it replaces. Returns the held sale id.
        /// The promotions are not kept: they are worked out again when the sale is recalled.
        /// </summary>
        public async Task<int> HoldSaleAsync(IEnumerable<PosLine> lines, int? replaceHeldId, int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", userId);
            parameters.Add("p_lines", new JsonbParameter(ToJsonLines(lines)));
            parameters.Add("p_replace_held_id", replaceHeldId, DbType.Int32);

            return await GetSingleAsync<int>(Procedures.HoldSale, parameters);
        }

        public async Task<IEnumerable<HeldSaleModel>> GetHeldSalesAsync()
        {
            return await GetListAsync<HeldSaleModel>(Procedures.PosGetHeldSales);
        }

        public async Task<IEnumerable<HeldLineModel>> GetHeldLinesAsync(int saleId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_sale_id", saleId);

            return await GetListAsync<HeldLineModel>(Procedures.PosGetHeldLines, parameters);
        }

        /// <summary>Discards a held sale; false when it no longer exists.</summary>
        public async Task<bool> DeleteHeldAsync(int saleId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_sale_id", saleId);

            return await GetSingleAsync<bool>(Procedures.PosDeleteHeld, parameters);
        }

        /// <summary>
        /// Saves a return invoice (R-...) from the scanned lines: the money goes back, the stock comes back in.
        /// method: PaymentMethod.Cash or Card; currency "LBP" or "USD" (card: LBP only). All or nothing.
        /// </summary>
        public async Task<ReturnResultModel> SaveReturnAsync(IEnumerable<PosLine> lines, short method, string currency, int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", userId);
            parameters.Add("p_lines", new JsonbParameter(ToJsonLines(lines)));
            parameters.Add("p_refund_method", (int)method);
            parameters.Add("p_refund_currency", currency);

            return (await GetSingleAsync<ReturnResultModel>(Procedures.SaveReturn, parameters))!;
        }

        /// <summary>Finished sales and returns between two dates (inclusive), newest first. userId null = every cashier.</summary>
        public async Task<IEnumerable<SaleHistoryModel>> GetSalesByUserAsync(int? userId, DateTime from, DateTime to)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", userId, DbType.Int32);
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);

            return await GetListAsync<SaleHistoryModel>(Procedures.SalesByUser, parameters);
        }

        /// <summary>The items of one sale or return.</summary>
        public async Task<IEnumerable<SaleHistoryLineModel>> GetSaleLinesAsync(int saleId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_sale_id", saleId);

            return await GetListAsync<SaleHistoryLineModel>(Procedures.SaleGetLines, parameters);
        }

        /// <summary>Writes a row in audit_log for the logged-in user (e.g. who approved a refund).</summary>
        public async Task AddAuditAsync(string action, string tableName, int recordId, object details)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_action", action);
            parameters.Add("p_table_name", tableName);
            parameters.Add("p_record_id", recordId);
            parameters.Add("p_details", new JsonbParameter(details));

            await ExecuteAsync(Procedures.AddAudit, parameters);
        }

        // { packaging_id, quantity, unit_price, discount } as fn_hold_sale / fn_save_return expect.
        // Without the promotions: a held sale gets them again when it is recalled, a return has none.
        private static IEnumerable<object> ToJsonLines(IEnumerable<PosLine> lines) =>
            lines.Select(l => new { l.PackagingId, l.Quantity, l.UnitPrice, l.Discount }).ToList();
    }
}
