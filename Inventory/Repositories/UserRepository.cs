using System.Data;
using Dapper;
using Inventory.Models;

namespace Inventory.Repositories
{
    // Users of the system (Settings > Users). They log in with a password only.
    public class UserRepository : BaseRepository
    {
        public UserRepository(string? connectionString = null) : base(connectionString) { }

        public async Task<IEnumerable<UserModel>> GetAllAsync()
        {
            return await GetListAsync<UserModel>(Procedures.GetAllUsers);
        }

        /// <summary>
        /// Adds (user.UserId 0) or updates a user; returns its id.
        /// password null = keep the current one (a new user needs one). It is hashed here before it is sent.
        /// Fails when another active user already has this password.
        /// </summary>
        public async Task<int> SaveAsync(UserModel user, string? password)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", user.UserId == 0 ? null : user.UserId, DbType.Int32);
            parameters.Add("p_full_name", user.FullName);
            parameters.Add("p_password_hash", string.IsNullOrEmpty(password) ? null : PasswordHasher.Hash(password), DbType.String);
            parameters.Add("p_role", user.Role, DbType.Int16);
            parameters.Add("p_is_active", user.IsActive);

            return await GetSingleAsync<int>(Procedures.SaveUser, parameters);
        }

        /// <summary>Login: the active user with this password, or null when it is wrong.</summary>
        public async Task<UserModel?> FindByPasswordAsync(string password)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_password_hash", PasswordHasher.Hash(password));

            return await GetSingleAsync<UserModel>(Procedures.FindUserByPassword, parameters);
        }

        /// <summary>Deletes a user; one with activity is deactivated instead. Returns true when deleted.</summary>
        public async Task<bool> DeleteAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", userId);

            return await GetSingleAsync<string>(Procedures.DeleteUser, parameters) == "deleted";
        }
    }
}
