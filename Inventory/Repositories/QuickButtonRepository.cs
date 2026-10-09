using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Cashier quick buttons (items without a barcode) and their groups, managed in the back office
    public class QuickButtonRepository : BaseRepository
    {
        public QuickButtonRepository(string? connectionString = null) : base(connectionString) { }

        #region Groups

        /// <summary>Groups in order. forCashier = only the active ones that have buttons the cashier can sell.</summary>
        public async Task<IEnumerable<QuickGroupModel>> GetGroupsAsync(bool forCashier)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_for_cashier", forCashier);

            return await GetListAsync<QuickGroupModel>(Procedures.GetQuickGroups, parameters);
        }

        /// <summary>Adds (groupId null, goes to the end) or updates a group; returns its id.</summary>
        public async Task<int> SaveGroupAsync(int? groupId, string name, string? color, bool isActive)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_group_id", groupId, DbType.Int32);
            parameters.Add("p_name", name);
            parameters.Add("p_color", color, DbType.String);
            parameters.Add("p_is_active", isActive);

            return await GetSingleAsync<int>(Procedures.SaveQuickGroup, parameters);
        }

        /// <summary>Removes a group; its buttons are removed too, or kept without a group.</summary>
        public async Task DeleteGroupAsync(int groupId, bool deleteButtons)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_group_id", groupId);
            parameters.Add("p_delete_buttons", deleteButtons);

            await ExecuteAsync(Procedures.DeleteQuickGroup, parameters);
        }

        /// <summary>Saves the order of the groups (first id = first group).</summary>
        public async Task ReorderGroupsAsync(IEnumerable<int> groupIds)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_group_ids", groupIds.ToArray());

            await ExecuteAsync(Procedures.ReorderQuickGroups, parameters);
        }

        #endregion

        #region Buttons

        /// <summary>Buttons in order. forCashier = only the active ones whose item can be sold.</summary>
        public async Task<IEnumerable<QuickButtonModel>> GetAllAsync(bool forCashier)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_for_cashier", forCashier);

            return await GetListAsync<QuickButtonModel>(Procedures.GetQuickButtons, parameters);
        }

        /// <summary>Saves a button's label, tab, color, ask price and show (moving it to another tab puts it at the end).</summary>
        public async Task<int> SaveAsync(QuickButtonModel button)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_button_id", button.ButtonId == 0 ? null : button.ButtonId, DbType.Int32);
            parameters.Add("p_group_id", button.GroupId, DbType.Int32);
            parameters.Add("p_packaging_id", button.PackagingId);
            parameters.Add("p_caption", button.Caption, DbType.String);
            parameters.Add("p_color", button.Color, DbType.String);
            parameters.Add("p_ask_price", button.AskPrice);
            parameters.Add("p_is_active", button.IsActive);

            return await GetSingleAsync<int>(Procedures.SaveQuickButton, parameters);
        }

        /// <summary>Adds a button at the end of the tab for each packaging (in order); skips items that have one. Returns how many.</summary>
        public async Task<int> AddItemsAsync(int? groupId, IEnumerable<int> packagingIds)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_group_id", groupId, DbType.Int32);
            parameters.Add("p_packaging_ids", packagingIds.ToArray());

            return await GetSingleAsync<int>(Procedures.AddQuickButtonItems, parameters);
        }

        /// <summary>Sellable items for "Add items", by search text and/or category (with its sub-categories).</summary>
        public async Task<IEnumerable<QuickItemModel>> GetItemListAsync(string? search, int? categoryId, int limit = 500)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_search", search, DbType.String);
            parameters.Add("p_category_id", categoryId, DbType.Int32);
            parameters.Add("p_limit", limit);

            return await GetListAsync<QuickItemModel>(Procedures.QuickItemList, parameters);
        }

        public async Task DeleteAsync(int buttonId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_button_id", buttonId);

            await ExecuteAsync(Procedures.DeleteQuickButton, parameters);
        }

        /// <summary>Saves the order of the buttons of one tab (first id = first button).</summary>
        public async Task ReorderAsync(IEnumerable<int> buttonIds)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_button_ids", buttonIds.ToArray());

            await ExecuteAsync(Procedures.ReorderQuickButtons, parameters);
        }

        #endregion
    }
}
