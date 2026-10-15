using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Promotions (Item > Promotions; the cashier applies the ones running today)
    public class PromotionRepository : BaseRepository
    {
        public PromotionRepository(string? connectionString = null) : base(connectionString) { }

        /// <summary>Every promotion, newest first.</summary>
        public async Task<IEnumerable<PromotionModel>> GetAllAsync()
        {
            return await GetListAsync<PromotionModel>(Procedures.GetPromotions);
        }

        /// <summary>The promotions running today, each with the items it is for (PackagingIds).</summary>
        public async Task<IEnumerable<PromotionModel>> GetActiveAsync()
        {
            return await GetListAsync<PromotionModel>(Procedures.GetActivePromotions);
        }

        /// <summary>The items of one promotion, by name.</summary>
        public async Task<IEnumerable<PromotionItemModel>> GetItemsAsync(int promotionId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_promotion_id", promotionId);

            return await GetListAsync<PromotionItemModel>(Procedures.GetPromotionItems, parameters);
        }

        /// <summary>Adds (PromotionId 0) or updates a promotion with its items; returns its id.</summary>
        public async Task<int> SaveAsync(PromotionModel promotion, IEnumerable<int> packagingIds)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_promotion_id", promotion.PromotionId == 0 ? null : promotion.PromotionId, DbType.Int32);
            parameters.Add("p_name", promotion.Name);
            parameters.Add("p_promo_type", (int)promotion.PromoType);
            parameters.Add("p_starts_on", promotion.StartsOn?.Date, DbType.Date);
            parameters.Add("p_ends_on", promotion.EndsOn?.Date, DbType.Date);
            parameters.Add("p_is_active", promotion.IsActive);
            parameters.Add("p_new_price", promotion.NewPrice, DbType.Decimal);
            parameters.Add("p_percent", promotion.Percent, DbType.Decimal);
            parameters.Add("p_buy_qty", promotion.BuyQty, DbType.Int32);
            parameters.Add("p_free_qty", promotion.FreeQty, DbType.Int32);
            parameters.Add("p_group_price", promotion.GroupPrice, DbType.Decimal);
            parameters.Add("p_min_total", promotion.MinTotal, DbType.Decimal);
            parameters.Add("p_category_id", promotion.CategoryId, DbType.Int32);
            parameters.Add("p_packaging_ids", packagingIds.ToArray());

            return await GetSingleAsync<int>(Procedures.SavePromotion, parameters);
        }

        public async Task DeleteAsync(int promotionId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_promotion_id", promotionId);

            await ExecuteAsync(Procedures.DeletePromotion, parameters);
        }
    }
}
