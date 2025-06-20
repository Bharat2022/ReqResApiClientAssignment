using ReqResApiClient.Models;

namespace ReqResApiClient.Services
{
    public interface IUserService
    {
        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The User object, or null if not found or an error occurred.</returns>
        Task<User> GetUserById(int userId);

        /// <summary>
        /// Retrieves all users, handling pagination internally.
        /// </summary>
        /// <returns>A list of all users. Returns an empty list if no users are found or an error occurs.</returns>
        Task<List<User>> GetAllUsers();
    }
}
   