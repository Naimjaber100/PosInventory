using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Stock adjustments: drafts and posted ones (Database/stock_adjustment_functions.sql)
    public class StockAdjustmentRepository : BaseRepository
    {
        /// <summary>Every draft, and the adjustments made between two days (inclusive): drafts first, then newest first.</summary>
        public async Task<IEnumerable<StockAdjustmentModel>> GetAllAsync(DateTime from, DateTime to)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);
            return await GetListAsync<StockAdjustmentModel>(Procedures.GetStockAdjustments, parameters);
        }

        public async Task<IEnumerable<StockAdjustmentLineModel>> GetLinesAsync(int adjustmentId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_adjustment_id", adjustmentId);
            return await GetListAsync<StockAdjustmentLineModel>(Procedures.GetStockAdjustmentLines, parameters);
        }

        /// <summary>Standard items whose barcode is the search (exact), else whose name has every word of it.</summary>
        public async Task<IEnumerable<StockItemModel>> FindItemsAsync(string search)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_search", search, DbType.String);
            return await GetListAsync<StockItemModel>(Procedures.FindStockAdjustmentItems, parameters);
        }

        /// <summary>Adds (adjustmentId null) or changes a draft; post = true also posts it. Returns its id.</summary>
        public async Task<int> SaveAsync(int? adjustmentId, string reason, IEnumerable<StockAdjustmentLineModel> lines, bool post, int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_adjustment_id", adjustmentId, DbType.Int32);
            parameters.Add("p_reason", reason);
            parameters.Add("p_lines", new JsonbParameter(lines.Select(l => new { product_id = l.ProductId, counted_qty = l.CountedQty, qty_before = l.QtyBefore })));
            parameters.Add("p_post", post);
            parameters.Add("p_user_id", userId);
            return await GetSingleAsync<int>(Procedures.SaveStockAdjustment, parameters);
        }

        /// <summary>Deletes a draft (a posted adjustment cannot be deleted).</summary>
        public async Task DeleteAsync(int adjustmentId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_adjustment_id", adjustmentId);
            await ExecuteAsync(Procedures.DeleteStockAdjustment, parameters);
        }
    }
}
