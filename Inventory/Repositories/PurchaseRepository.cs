using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    public class PurchaseRepository : BaseRepository
    {
        public PurchaseRepository(string? connectionString = null) : base(connectionString) { }

        /// <summary>One page of purchase invoices, newest first. search: number, supplier invoice no. or supplier name.</summary>
        public async Task<PagedResult<PurchaseInvoiceModel>> GetPageAsync(int page, int pageSize, string? search = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_search", string.IsNullOrWhiteSpace(search) ? null : search.Trim(), DbType.String);

            return await GetPagedAsync<PurchaseInvoiceModel>(Procedures.GetAllPurchases, page, pageSize, parameters);
        }

        /// <summary>The item lines of one purchase invoice.</summary>
        public async Task<IEnumerable<PurchaseLineModel>> GetLinesAsync(int purchaseId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_purchase_id", purchaseId);

            return await GetListAsync<PurchaseLineModel>(Procedures.GetPurchaseLines, parameters);
        }

        /// <summary>The header of one purchase invoice (null when it does not exist).</summary>
        public async Task<PurchaseHeaderModel?> GetByIdAsync(int purchaseId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_purchase_id", purchaseId);

            return await GetSingleAsync<PurchaseHeaderModel>(Procedures.GetPurchaseById, parameters);
        }

        /// <summary>Packagings that can be purchased, by exact barcode, barcode start or name words.</summary>
        public async Task<IEnumerable<PackagingLookupModel>> FindPackagingAsync(string search, int limit = 50)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_search", search);
            parameters.Add("p_limit", limit);

            return await GetListAsync<PackagingLookupModel>(Procedures.FindPurchasePackaging, parameters);
        }

        /// <summary>
        /// The packagings that best match a line read from an invoice photo or a voice note (fuzzy name, packaging, barcode),
        /// best first. textAlt = the same name in the other script (Arabic / Latin): the better of the two counts.
        /// </summary>
        public async Task<List<PackagingLookupModel>> MatchPackagingAsync(string text, string? packaging, string? barcode, int limit = 5, string? textAlt = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_text", text);
            parameters.Add("p_text_alt", textAlt, DbType.String);
            parameters.Add("p_packaging", packaging, DbType.String);
            parameters.Add("p_barcode", barcode, DbType.String);
            parameters.Add("p_limit", limit);

            return (await GetListAsync<PackagingLookupModel>(Procedures.MatchPurchasePackaging, parameters)).ToList();
        }

        /// <summary>
        /// Saves a new purchase or return (header.PurchaseId = 0) or updates a draft, with all its lines.
        /// post = true also posts it (purchase: stock up and average cost updated; return: stock and the supplier
        /// balance go down; number assigned, no more changes). Returns the purchase_id.
        /// </summary>
        public async Task<int> SaveAsync(PurchaseHeaderModel header, IEnumerable<PurchaseEditLine> lines, bool post, int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_purchase_id", header.PurchaseId == 0 ? null : header.PurchaseId, DbType.Int32);
            parameters.Add("p_supplier_id", header.SupplierId, DbType.Int32);
            parameters.Add("p_supplier_invoice_no", header.SupplierInvoiceNo, DbType.String);
            parameters.Add("p_invoice_date", header.InvoiceDate.Date, DbType.Date);
            parameters.Add("p_discount", header.Discount);
            parameters.Add("p_notes", header.Notes, DbType.String);
            parameters.Add("p_lines", new JsonbParameter(lines.Select(l => new { l.PackagingId, l.Quantity, l.UnitCost })));
            parameters.Add("p_post", post);
            parameters.Add("p_user_id", userId);
            parameters.Add("p_currency", header.Currency, DbType.String);
            parameters.Add("p_currency_rate", header.CurrencyRate, DbType.Decimal);
            parameters.Add("p_doc_type", (int)header.DocType);
            parameters.Add("p_return_of_id", header.ReturnOfId, DbType.Int32);

            return await GetSingleAsync<int>(Procedures.SavePurchase, parameters);
        }
    }
}
