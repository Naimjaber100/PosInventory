using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Reports (Database/report_functions.sql): the database adds up, the app shows the rows
    public class ReportRepository : BaseRepository
    {
        public ReportRepository(string? connectionString = null) : base(connectionString) { }

        /// <summary>Report 1: one row per day from 'from' to 'to' (days without sales too). userId null = every cashier.</summary>
        public async Task<List<DailySalesRow>> GetDailySalesAsync(DateTime from, DateTime to, int? userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);
            parameters.Add("p_user_id", userId, DbType.Int32);

            return (await GetListAsync<DailySalesRow>(Procedures.ReportDailySales, parameters)).ToList();
        }

        /// <summary>Report 2: one row per item sold or returned in the period, best selling first. userId null = every cashier.</summary>
        public async Task<List<SalesByItemRow>> GetSalesByItemAsync(DateTime from, DateTime to, int? userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);
            parameters.Add("p_user_id", userId, DbType.Int32);

            return (await GetListAsync<SalesByItemRow>(Procedures.ReportSalesByItem, parameters)).ToList();
        }

        /// <summary>Report 3: one row per sale / return invoice in the period, in time order. userId null = every cashier.</summary>
        public async Task<List<SalesByInvoiceRow>> GetSalesByInvoiceAsync(DateTime from, DateTime to, int? userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);
            parameters.Add("p_user_id", userId, DbType.Int32);

            return (await GetListAsync<SalesByInvoiceRow>(Procedures.ReportSalesByInvoice, parameters)).ToList();
        }

        /// <summary>Report 5: one row per client who still owes something now, biggest amount first.</summary>
        public async Task<List<UnpaidByCustomerRow>> GetUnpaidByCustomerAsync()
        {
            return (await GetListAsync<UnpaidByCustomerRow>(Procedures.ReportUnpaidByCustomer)).ToList();
        }

        /// <summary>Report 6: the unpaid sales and payments of a client in the period, in time order. customerId null = every client.</summary>
        public async Task<List<CustomerStatementRow>> GetCustomerStatementAsync(DateTime from, DateTime to, int? customerId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);
            parameters.Add("p_customer_id", customerId, DbType.Int32);

            return (await GetListAsync<CustomerStatementRow>(Procedures.ReportCustomerStatement, parameters)).ToList();
        }

        /// <summary>Report 4: one row per day with its sales and what is still unpaid of them. userId null = every cashier.</summary>
        public async Task<List<SalesUnpaidRow>> GetSalesUnpaidAsync(DateTime from, DateTime to, int? userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);
            parameters.Add("p_user_id", userId, DbType.Int32);

            return (await GetListAsync<SalesUnpaidRow>(Procedures.ReportSalesUnpaid, parameters)).ToList();
        }

        /// <summary>Report 7: one row per posted purchase in the period, in date order. supplierId null = every supplier.</summary>
        public async Task<List<PurchasesByInvoiceRow>> GetPurchasesByInvoiceAsync(DateTime from, DateTime to, int? supplierId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);
            parameters.Add("p_supplier_id", supplierId, DbType.Int32);

            return (await GetListAsync<PurchasesByInvoiceRow>(Procedures.ReportPurchasesByInvoice, parameters)).ToList();
        }

        /// <summary>Report 8: one row per item bought in the period, biggest cost first. supplierId null = every supplier.</summary>
        public async Task<List<PurchasesByItemRow>> GetPurchasesByItemAsync(DateTime from, DateTime to, int? supplierId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);
            parameters.Add("p_supplier_id", supplierId, DbType.Int32);

            return (await GetListAsync<PurchasesByItemRow>(Procedures.ReportPurchasesByItem, parameters)).ToList();
        }

        /// <summary>Report 9: one row per supplier with purchases or payments in the period, biggest purchases first.</summary>
        public async Task<List<PurchasesBySupplierRow>> GetPurchasesBySupplierAsync(DateTime from, DateTime to)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);

            return (await GetListAsync<PurchasesBySupplierRow>(Procedures.ReportPurchasesBySupplier, parameters)).ToList();
        }
    }
}
