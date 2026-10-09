using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // The one company record (Database/company_functions.sql)
    public class CompanyRepository : BaseRepository
    {
        public const string DefaultName = "My Store";

        public CompanyRepository(string? connectionString = null) : base(connectionString) { }

        public async Task<CompanyModel> GetAsync()
        {
            return await GetSingleAsync<CompanyModel>(Procedures.GetCompany) ?? new CompanyModel { Name = DefaultName };
        }

        /// <summary>The company name for screen headers ("My Store" when it cannot be read).</summary>
        public async Task<string> GetNameAsync()
        {
            try
            {
                var name = (await GetAsync()).Name;
                return string.IsNullOrWhiteSpace(name) ? DefaultName : name;
            }
            catch
            {
                return DefaultName;
            }
        }

        public async Task SaveAsync(CompanyModel company)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_name", company.Name);
            parameters.Add("p_address", company.Address, DbType.String);
            parameters.Add("p_email", company.Email, DbType.String);
            parameters.Add("p_mobile", company.Mobile, DbType.String);
            parameters.Add("p_vat_number", company.VatNumber, DbType.String);

            await ExecuteAsync(Procedures.SaveCompany, parameters);
        }
    }
}
