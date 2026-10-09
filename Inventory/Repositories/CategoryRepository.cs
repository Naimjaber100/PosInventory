using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    public class CategoryRepository : BaseRepository
    {
        public CategoryRepository(string? connectionString = null) : base(connectionString) { }

        /// <summary>All categories, parents before children, with their full path.</summary>
        public async Task<IEnumerable<CategoryModel>> GetAllAsync()
        {
            return await GetListAsync<CategoryModel>(Procedures.GetAllCategories);
        }

        /// <summary>Adds a category under parentId (null = top level); returns the new category_id.</summary>
        public async Task<int> AddAsync(string name, int? parentId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_name", name);
            parameters.Add("p_parent_id", parentId, DbType.Int32);

            return await GetSingleAsync<int>(Procedures.AddCategory, parameters);
        }

        /// <summary>Renames and/or moves a category under another parent (null = top level).</summary>
        public async Task UpdateAsync(int categoryId, string name, int? parentId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_category_id", categoryId);
            parameters.Add("p_name", name);
            parameters.Add("p_parent_id", parentId, DbType.Int32);

            await ExecuteAsync(Procedures.UpdateCategory, parameters);
        }

        /// <summary>Deletes an empty category (fails when it has sub-categories or items).</summary>
        public async Task DeleteAsync(int categoryId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_category_id", categoryId);

            await ExecuteAsync(Procedures.DeleteCategory, parameters);
        }
    }
}
