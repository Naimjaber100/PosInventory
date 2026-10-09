using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    public class ProductRepository : BaseRepository
    {
        public ProductRepository(string? connectionString = null) : base(connectionString) { }

        public const int NoCategory = 0;

        /// <summary>
        /// One page of products. categoryId: null = all items, NoCategory (0) = items without a category,
        /// otherwise that category and all of its sub-categories.
        /// search: name words or barcode (null/blank = no search).
        /// </summary>
        public async Task<PagedResult<ProductModel>> GetPageAsync(int page, int pageSize, int? categoryId = null, string? search = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_category_id", categoryId, DbType.Int32);
            parameters.Add("p_search", string.IsNullOrWhiteSpace(search) ? null : search.Trim(), DbType.String);

            return await GetPagedAsync<ProductModel>(Procedures.GetAllProducts, page, pageSize, parameters);
        }

        public async Task<ProductModel?> GetByIdAsync(int productId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_product_id", productId);

            return await GetSingleAsync<ProductModel>(Procedures.GetProductById, parameters);
        }

        /// <summary>The active packagings of one item (default first).</summary>
        public async Task<IEnumerable<PackagingModel>> GetPackagingsAsync(int productId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_product_id", productId);

            return await GetListAsync<PackagingModel>(Procedures.GetProductPackagings, parameters);
        }

        /// <summary>
        /// Adds the product with its packagings and opening stock; returns the new product_id.
        /// packagings null = one default packaging from the base unit, SalePrice and Barcode.
        /// </summary>
        public async Task<int> AddAsync(ProductModel product, decimal openingQty, int userId, IEnumerable<PackagingModel>? packagings = null)
        {
            var parameters = ProductParameters(product, packagings);
            parameters.Add("p_avg_cost", product.AvgCost);
            parameters.Add("p_opening_qty", openingQty);
            parameters.Add("p_user_id", userId);

            return await GetSingleAsync<int>(Procedures.AddProduct, parameters);
        }

        /// <summary>
        /// Updates the product. packagings replaces all its packagings (null = only the default
        /// packaging's SalePrice and Barcode change).
        /// </summary>
        public async Task UpdateAsync(ProductModel product, IEnumerable<PackagingModel>? packagings = null)
        {
            var parameters = ProductParameters(product, packagings);
            parameters.Add("p_product_id", product.ProductId);

            await ExecuteAsync(Procedures.UpdateProduct, parameters);
        }

        /// <summary>Returns true when deleted, false when the item has history and was deactivated instead.</summary>
        public async Task<bool> DeleteAsync(int productId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_product_id", productId);

            return await GetSingleAsync<bool>(Procedures.DeleteProduct, parameters);
        }

        /// <summary>Imports all rows in one transaction; returns the number imported.</summary>
        public async Task<int> ImportAsync(IEnumerable<ProductImportRow> rows, int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_rows", new JsonbParameter(rows));
            parameters.Add("p_user_id", userId);

            return await GetSingleAsync<int>(Procedures.ImportProducts, parameters);
        }

        // Arguments shared by fn_product_add and fn_product_update
        private static DynamicParameters ProductParameters(ProductModel product, IEnumerable<PackagingModel>? packagings)
        {
            var list = packagings?.ToList();
            if (list != null)
            {
                // The default packaging is what the item list shows as price / barcode
                var main = list.FirstOrDefault(p => p.IsDefault);
                product.SalePrice = main?.SalePrice ?? 0;
                product.Barcode = main?.Barcode;
            }

            var parameters = new DynamicParameters();
            parameters.Add("p_name", product.Name);
            parameters.Add("p_category_id", product.CategoryId, DbType.Int32);
            parameters.Add("p_product_type", product.ProductType);
            parameters.Add("p_size", product.Size, DbType.String);
            parameters.Add("p_color", product.Color, DbType.String);
            parameters.Add("p_base_unit", product.BaseUnit);
            parameters.Add("p_is_weighted", product.IsWeighted);
            parameters.Add("p_reorder_level", product.ReorderLevel);
            parameters.Add("p_is_active", product.IsActive);
            parameters.Add("p_sale_price", product.SalePrice, DbType.Decimal);
            parameters.Add("p_barcode", product.Barcode, DbType.String);
            if (list != null) parameters.Add("p_packagings", new JsonbParameter(list));
            return parameters;
        }
    }
}
