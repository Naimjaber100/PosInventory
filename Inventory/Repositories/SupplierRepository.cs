using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Suppliers (suppliers table, Contacts > Suppliers).
    public class SupplierRepository : BaseRepository
    {
        public SupplierRepository(string? connectionString = null) : base(connectionString) { }

        /// <summary>Suppliers by name / phone / email / VAT number (null = all), active first.</summary>
        public async Task<IEnumerable<SupplierModel>> GetAllAsync(string? search = null, bool includeInactive = false)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_search", search, DbType.String);
            parameters.Add("p_include_inactive", includeInactive);

            return await GetListAsync<SupplierModel>(Procedures.GetSuppliers, parameters);
        }

        /// <summary>Adds (SupplierId 0) or updates a supplier; returns its id. The balance is not saved here.</summary>
        public async Task<int> SaveAsync(SupplierModel supplier)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_supplier_id", supplier.SupplierId == 0 ? null : supplier.SupplierId, DbType.Int32);
            parameters.Add("p_name", supplier.Name);
            parameters.Add("p_phone", supplier.Phone, DbType.String);
            parameters.Add("p_address", supplier.Address, DbType.String);
            parameters.Add("p_email", supplier.Email, DbType.String);
            parameters.Add("p_vat_number", supplier.VatNumber, DbType.String);
            parameters.Add("p_notes", supplier.Notes, DbType.String);
            parameters.Add("p_is_active", supplier.IsActive);

            return await GetSingleAsync<int>(Procedures.SaveSupplier, parameters);
        }

        /// <summary>Deletes a supplier; one with purchases or payments is deactivated instead. Returns true when deleted.</summary>
        public async Task<bool> DeleteAsync(int supplierId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_supplier_id", supplierId);

            return await GetSingleAsync<string>(Procedures.DeleteSupplier, parameters) == "deleted";
        }
    }
}
