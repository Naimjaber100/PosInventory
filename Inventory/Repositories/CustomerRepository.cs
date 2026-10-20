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

        /// <summary>The unpaid and partly paid invoices of a client, oldest first (the order payments settle them).</summary>
        public async Task<IEnumerable<CustomerOpenInvoiceModel>> GetOpenInvoicesAsync(int customerId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_customer_id", customerId);

            return await GetListAsync<CustomerOpenInvoiceModel>(Procedures.CustomerOpenInvoices, parameters);
        }

        /// <summary>
        /// A client pays (part of) what they owe: the money settles their invoices oldest first.
        /// method: PaymentMethod.Cash or Card. More than they owe is refused. All or nothing.
        /// </summary>
        public async Task<CustomerPaymentResultModel> ReceivePaymentAsync(int customerId, decimal amount, string currency, short method, int userId)
        {
            // In the user's open shift: cash received goes into their drawer count
            return await ShiftRepository.InCurrentShiftAsync(userId, async shiftId =>
            {
                var parameters = new DynamicParameters();
                parameters.Add("p_customer_id", customerId);
                parameters.Add("p_amount", amount);
                parameters.Add("p_currency", currency);
                parameters.Add("p_method", (int)method);
                parameters.Add("p_user_id", userId);
                parameters.Add("p_shift_id", shiftId, DbType.Int32);

                return (await GetSingleAsync<CustomerPaymentResultModel>(Procedures.ReceiveCustomerPayment, parameters))!;
            });
        }

        /// <summary>The payments of a client, newest first.</summary>
        public async Task<IEnumerable<CustomerPaymentModel>> GetPaymentsAsync(int customerId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_customer_id", customerId);

            return await GetListAsync<CustomerPaymentModel>(Procedures.GetCustomerPayments, parameters);
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
