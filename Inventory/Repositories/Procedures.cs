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

        // Reports (Database/report_functions.sql)
        public const string ReportDailySales = "fn_report_daily_sales";
        public const string ReportSalesByItem = "fn_report_sales_by_item";
        public const string ReportSalesByInvoice = "fn_report_sales_by_invoice";
        public const string ReportSalesUnpaid = "fn_report_sales_unpaid";
        public const string ReportUnpaidByCustomer = "fn_report_unpaid_by_customer";
        public const string ReportCustomerStatement = "fn_report_customer_statement";
        public const string ReportPurchasesByInvoice = "fn_report_purchases_by_invoice";
        public const string ReportPurchasesByItem = "fn_report_purchases_by_item";
        public const string ReportPurchasesBySupplier = "fn_report_purchases_by_supplier";
        public const string ReportStockValue = "fn_report_stock_value";

        // Company, one record (Database/company_functions.sql)
        public const string GetCompany = "fn_company_get";
        public const string SaveCompany = "fn_company_save";
        public const string GetCompanyLogo = "fn_company_get_logo";
        public const string SetCompanyLogo = "fn_company_set_logo";

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
        public const string SetProductActive = "fn_product_set_active";
        public const string GetProductScaleCode = "fn_product_get_scale_code";   // Database/scale_functions.sql
        public const string SetProductScaleCode = "fn_product_set_scale_code";
        public const string PosFindScaleItem = "fn_pos_find_scale_item";
        public const string ImportProducts = "fn_product_import";
        public const string GetProductPackagings = "fn_product_get_packagings";

        // Purchases
        public const string GetAllPurchases = "fn_purchase_get_all";
        public const string GetPurchaseLines = "fn_purchase_get_lines";
        public const string GetPurchaseById = "fn_purchase_get_by_id";
        public const string FindPurchasePackaging = "fn_purchase_find_packaging";
        public const string SavePurchase = "fn_purchase_save";        public const string MatchPurchasePackaging = "fn_purchase_match_packaging";

        // Cashier / sales (Database/pos_functions.sql; fn_save_sale, fn_hold_sale, fn_current_rate were already in the database)
        public const string PosFindItem = "fn_pos_find_item";
        public const string PosItemList = "fn_pos_item_list";   // the cashier's item list in memory (PosItemCache)
        public const string PosGetHeldSales = "fn_pos_get_held_sales";
        public const string PosGetHeldLines = "fn_pos_get_held_lines";
        public const string PosDeleteHeld = "fn_pos_delete_held";
        public const string SaveReturn = "fn_save_return";
        public const string SalesByUser = "fn_sales_by_user";
        public const string SaleGetLines = "fn_sale_get_lines";
        public const string SaleReceipt = "fn_sale_receipt";   // Database/receipt_functions.sql
        public const string AddAudit = "fn_audit_add";
        public const string SaveSale = "fn_save_sale";
        public const string HoldSale = "fn_hold_sale";
        public const string GetSetting = "fn_setting";
        public const string SetSetting = "fn_setting_set";   // Database/quick_button_functions.sql

        // Promotions (Database/promotion_functions.sql)
        public const string GetPromotions = "fn_promotion_get_all";
        public const string GetActivePromotions = "fn_promotion_get_active";
        public const string GetPromotionItems = "fn_promotion_get_items";
        public const string SavePromotion = "fn_promotion_save";
        public const string DeletePromotion = "fn_promotion_delete";

        // Clients and suppliers (Database/party_functions.sql)
        public const string GetCustomers = "fn_customer_get_all";
        public const string SaveCustomer = "fn_customer_save";
        public const string DeleteCustomer = "fn_customer_delete";
        public const string GetSuppliers = "fn_supplier_get_all";
        public const string SaveSupplier = "fn_supplier_save";
        public const string DeleteSupplier = "fn_supplier_delete";

        // Unpaid sales and the payments that settle them (Database/customer_payment_functions.sql)
        public const string CustomerOpenInvoices = "fn_customer_open_invoices";
        public const string ReceiveCustomerPayment = "fn_receive_customer_payment";
        public const string GetCustomerPayments = "fn_customer_get_payments";

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
