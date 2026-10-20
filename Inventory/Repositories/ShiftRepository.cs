using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Shifts, one open shift per user (Database/shift_functions.sql)
    public class ShiftRepository : BaseRepository
    {
        /// <summary>The shift the last sale / refund / payment of this program went into (shown on the cashier screen).</summary>
        public static int? LastShiftId { get; private set; }

        /// <summary>The user's open shift; opens one when they have none. Null when shift_functions.sql is not applied yet.</summary>
        public async Task<int?> CurrentAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", userId);
            try
            {
                LastShiftId = await GetSingleAsync<int>(Procedures.ShiftCurrent, parameters);
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == Npgsql.PostgresErrorCodes.UndefinedFunction)
            {
                LastShiftId = null;   // no shifts yet: sales are saved without one, as before
            }
            return LastShiftId;
        }

        /// <summary>
        /// Saves something in the user's open shift (opened when needed). When a manager ends that shift
        /// at the same moment, it is saved again in the user's new shift.
        /// </summary>
        public static async Task<T> InCurrentShiftAsync<T>(int userId, Func<int?, Task<T>> save)
        {
            var shifts = new ShiftRepository();
            try
            {
                return await save(await shifts.CurrentAsync(userId));
            }
            catch (Npgsql.PostgresException ex) when (ex.MessageText.StartsWith("This shift is closed", StringComparison.Ordinal))
            {
                return await save(await shifts.CurrentAsync(userId));
            }
        }

        /// <summary>Every open shift, and the shifts opened between two days (inclusive): open first, then newest first.</summary>
        public async Task<IEnumerable<ShiftModel>> GetListAsync(DateTime from, DateTime to)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_from", from.Date, DbType.Date);
            parameters.Add("p_to", to.Date, DbType.Date);
            return await GetListAsync<ShiftModel>(Procedures.ShiftList, parameters);
        }

        /// <summary>One shift's figures, worked out now when it is open.</summary>
        public async Task<ShiftModel?> GetSummaryAsync(int shiftId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_shift_id", shiftId);
            return await GetSingleAsync<ShiftModel>(Procedures.ShiftSummary, parameters);
        }

        /// <summary>Ends the shift with the cash counted in the drawer. Returns its final figures.</summary>
        public async Task<ShiftModel> CloseAsync(int shiftId, decimal countedLbp, decimal countedUsd)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_shift_id", shiftId);
            parameters.Add("p_counted_lbp", countedLbp);
            parameters.Add("p_counted_usd", countedUsd);
            return (await GetSingleAsync<ShiftModel>(Procedures.CloseShift, parameters))!;
        }
    }
}
