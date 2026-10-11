using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Clients (customers table, Contacts > Clients).
    public class CustomerRepository : BaseRepository
    {
        public CustomerRepository(string? connectionString = null) : base(connectionString) { }

        /// <summary>Clients by name / phone / email (null = all), active first.</summary>
        public async Task<IEnumerable<CustomerModel>> GetAllAsync(string? search = null, bool includeInactive = false)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_search", search, DbType.String);
            parameters.Add("p_include_inactive", includeInactive);

            return await GetListAsync<CustomerModel>(Procedures.GetCustomers, parameters);
        }

        /// <summary>Adds (CustomerId 0) or updates a client; returns its id. The balance is not saved here.</summary>
        public async Task<int> SaveAsync(CustomerModel customer)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_customer_id", customer.CustomerId == 0 ? null : customer.CustomerId, DbType.Int32);
            parameters.Add("p_name", customer.Name);
            parameters.Add("p_phone", customer.Phone, DbType.String);
            parameters.Add("p_address", customer.Address, DbType.String);
            parameters.Add("p_email", customer.Email, DbType.String);
            parameters.Add("p_credit_limit", customer.CreditLimitLbp, DbType.Decimal);
            parameters.Add("p_notes", customer.Notes, DbType.String);
            parameters.Add("p_is_active", customer.IsActive);

            return await GetSingleAsync<int>(Procedures.SaveCustomer, parameters);
        }

        /// <summary>Deletes a client; one with sales or payments is deactivated instead. Returns true when deleted.</summary>
        public async Task<bool> DeleteAsync(int customerId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_customer_id", customerId);

            return await GetSingleAsync<string>(Procedures.DeleteCustomer, parameters) == "deleted";
        }
    }
}
