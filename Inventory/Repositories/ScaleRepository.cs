using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Label scales and the items sent to them (Database/scale_functions.sql)
    public class ScaleRepository : BaseRepository
    {
        public ScaleRepository(string? connectionString = null) : base(connectionString) { }

        public async Task<IEnumerable<ScaleModel>> GetAllAsync() => await GetListAsync<ScaleModel>(Procedures.GetScales);

        /// <summary>Adds (ScaleId 0) or updates the scale; returns its scale_id.</summary>
        public async Task<int> SaveAsync(ScaleModel scale)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_scale_id", scale.ScaleId == 0 ? null : scale.ScaleId, DbType.Int32);
            parameters.Add("p_name", scale.Name);
            parameters.Add("p_driver", scale.Driver);
            parameters.Add("p_host", scale.Host, DbType.String);
            parameters.Add("p_port", scale.Port, DbType.Int32);
            parameters.Add("p_serial_port", scale.SerialPort, DbType.String);
            parameters.Add("p_baud_rate", scale.BaudRate, DbType.Int32);
            parameters.Add("p_file_path", scale.FilePath, DbType.String);
            parameters.Add("p_is_active", scale.IsActive);

            return await GetSingleAsync<int>(Procedures.SaveScale, parameters);
        }

        public async Task DeleteAsync(int scaleId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_scale_id", scaleId);

            await ExecuteAsync(Procedures.DeleteScale, parameters);
        }

        public async Task<IEnumerable<ScaleItemModel>> GetItemsAsync(int scaleId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_scale_id", scaleId);

            return await GetListAsync<ScaleItemModel>(Procedures.GetScaleItems, parameters);
        }

        /// <summary>Puts the items on the scale (an item without a PLU gets the next free one); returns how many were added.</summary>
        public async Task<int> AddItemsAsync(int scaleId, IEnumerable<int> productIds)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_scale_id", scaleId);
            parameters.Add("p_product_ids", productIds.Distinct().ToArray());
            parameters.Add("p_code_length", ScaleBarcode.Settings.ItemCodeLength);

            return await GetSingleAsync<int>(Procedures.AddScaleItems, parameters);
        }

        /// <summary>The item's PLU (the same on every scale and for the cashier) and its hot key on this scale.</summary>
        public async Task SaveItemAsync(int scaleId, int productId, string scaleCode, int? hotKey)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_scale_id", scaleId);
            parameters.Add("p_product_id", productId);
            parameters.Add("p_scale_code", scaleCode);
            parameters.Add("p_hot_key", hotKey, DbType.Int32);

            await ExecuteAsync(Procedures.SaveScaleItem, parameters);
        }

        public async Task RemoveItemsAsync(int scaleId, IEnumerable<int> productIds)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_scale_id", scaleId);
            parameters.Add("p_product_ids", productIds.ToArray());

            await ExecuteAsync(Procedures.RemoveScaleItems, parameters);
        }

        /// <summary>Remembers what was sent, so the list shows what changed since.</summary>
        public async Task MarkSentAsync(int scaleId, IEnumerable<Scales.ScalePlu> sent)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_scale_id", scaleId);
            parameters.Add("p_items", new JsonbParameter(sent.Select(p => new { p.ProductId, p.Name, p.Code, p.Price, p.HotKey })));

            await ExecuteAsync(Procedures.MarkScaleItemsSent, parameters);
        }
    }
}
