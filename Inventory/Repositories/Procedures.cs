namespace Inventory.Repositories
{
    // Database function names (see the scripts in the Database folder)
    public static class Procedures
    {
        // Currencies and exchange rates (Database/currency_functions.sql)
        public const string GetCurrencies = "fn_currency_get_all";
        public const string SaveCurrency = "fn_currency_save";
        public const string SetCurrencyRate = "fn_currency_set_rate";
        public const string GetCurrencyRates = "fn_currency_rates";
        public const string SetPrimaryCurrency = "fn_currency_set_primary";

        // Company, one record (Database/company_functions.sql)
        public const string GetCompany = "fn_company_get";
        public const string SaveCompany = "fn_company_save";

        // Users (Database/user_functions.sql)
        public const string GetAllUsers = "fn_user_get_all";
        public const string SaveUser = "fn_user_save";
        public const string DeleteUser = "fn_user_delete";
        public const string FindUserByPassword = "fn_user_find_by_password";

        // Categories
        public const string GetAllCategories = "fn_category_get_all";
        public const string AddCategory = "fn_category_add";
        public const string UpdateCategory = "fn_category_update";
        public const string DeleteCategory = "fn_category_delete";

        // Products
        public const string GetAllProducts = "fn_product_get_all";
        public const string GetProductById = "fn_product_get_by_id";
        public const string AddProduct = "fn_product_add";
        public const string UpdateProduct = "fn_product_update";
        public const string DeleteProduct = "fn_product_delete";
        public const string ImportProducts = "fn_product_import";
        public const string GetProductPackagings = "fn_product_get_packagings";

        // Purchases
        public const string GetAllPurchases = "fn_purchase_get_all";
        public const string GetPurchaseLines = "fn_purchase_get_lines";
        public const string GetPurchaseById = "fn_purchase_get_by_id";
        public const string FindPurchasePackaging = "fn_purchase_find_packaging";
        public const string SavePurchase = "fn_purchase_save";

        // Cashier / sales (Database/pos_functions.sql; fn_save_sale, fn_hold_sale, fn_current_rate were already in the database)
        public const string PosFindItem = "fn_pos_find_item";
        public const string PosGetHeldSales = "fn_pos_get_held_sales";
        public const string PosGetHeldLines = "fn_pos_get_held_lines";
        public const string PosDeleteHeld = "fn_pos_delete_held";
        public const string SaveReturn = "fn_save_return";
        public const string SalesByUser = "fn_sales_by_user";
        public const string SaleGetLines = "fn_sale_get_lines";
        public const string AddAudit = "fn_audit_add";
        public const string SaveSale = "fn_save_sale";
        public const string HoldSale = "fn_hold_sale";
        public const string GetSetting = "fn_setting";
        public const string SetSetting = "fn_setting_set";   // Database/quick_button_functions.sql

        // Cashier quick buttons and their groups (Database/quick_button_functions.sql)
        public const string GetQuickGroups = "fn_quick_group_get_all";
        public const string SaveQuickGroup = "fn_quick_group_save";
        public const string DeleteQuickGroup = "fn_quick_group_delete";
        public const string ReorderQuickGroups = "fn_quick_group_reorder";
        public const string GetQuickButtons = "fn_quick_button_get_all";
        public const string SaveQuickButton = "fn_quick_button_save";
        public const string DeleteQuickButton = "fn_quick_button_delete";
        public const string ReorderQuickButtons = "fn_quick_button_reorder";
        public const string AddQuickButtonItems = "fn_quick_button_add_items";
        public const string QuickItemList = "fn_quick_item_list";
    }
}
