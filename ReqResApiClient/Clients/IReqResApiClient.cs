using ReqResApiClient.Models;

namespace ReqResApiClient.Clients
{
    public interface IReqResApiClient
    {
        /// <summary>
        /// Fetches a single user by their ID.
        /// </summary>
        /// <param name="userId">The ID of the user to fetch.</param>
        /// <returns>A User object if found, otherwise null.</returns>
        Task<User> GetUserByIdAsync(int userId);

        /// <summary>
        /// Fetches a paginated list of users.
        /// </summary>
        /// <param name="page">The page number to fetch (default is 1).</param>
        /// <returns>A list of User objects.</returns>
        Task<List<User>> GetUsersAsync(int page = 1);

        /// <summary>
        /// Fetches all users by iterating through all pages.
        /// </summary>
        /// <returns>A complete list of all User objects.</returns>
        Task<List<User>> GetAllUsersAsync();
    }
}