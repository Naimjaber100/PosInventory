using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Currencies and exchange rates (Database/currency_functions.sql)
    public class CurrencyRepository : BaseRepository
    {
        public CurrencyRepository(string? connectionString = null) : base(connectionString) { }

        public async Task<IEnumerable<CurrencyModel>> GetAllAsync()
        {
            return await GetListAsync<CurrencyModel>(Procedures.GetCurrencies);
        }

        /// <summary>The currencies with their latest rates, as the screens use them.</summary>
        public async Task<CurrencySet> GetSetAsync() => new(await GetAllAsync());

        /// <summary>Adds or changes a currency (the code identifies it).</summary>
        public async Task SaveAsync(CurrencyModel currency)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_code", currency.Code);
            parameters.Add("p_name", currency.Name);
            parameters.Add("p_symbol", currency.Symbol);
            parameters.Add("p_decimals", currency.Decimals);
            parameters.Add("p_is_active", currency.IsActive);
            parameters.Add("p_sort_order", currency.SortOrder);

            await ExecuteAsync(Procedures.SaveCurrency, parameters);
        }

        /// <summary>Sets a new rate: 1 unit of the currency = rate of the primary currency (kept as history).</summary>
        public async Task SetRateAsync(string code, decimal rate, int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_code", code);
            parameters.Add("p_rate", rate);
            parameters.Add("p_user_id", userId);

            await ExecuteAsync(Procedures.SetCurrencyRate, parameters);
        }

        public async Task<IEnumerable<CurrencyRateModel>> GetRatesAsync(string code)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_code", code);

            return await GetListAsync<CurrencyRateModel>(Procedures.GetCurrencyRates, parameters);
        }

        /// <summary>Makes a currency the primary one (refused once there are sales or purchases).</summary>
        public async Task SetPrimaryAsync(string code)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_code", code);

            await ExecuteAsync(Procedures.SetPrimaryCurrency, parameters);
        }
    }
}
