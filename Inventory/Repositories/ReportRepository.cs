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
    }
}
