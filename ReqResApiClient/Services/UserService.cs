using ReqResApiClient.Clients;
using ReqResApiClient.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Caching.Memory;


namespace ReqResApiClient.Services
{
    public class UserService : IUserService
    {
        private readonly IReqResApiClient _apiClient;
        private readonly ILogger<UserService> _logger;
        private readonly IMemoryCache _cache;

        private const string UserCacheKeyPrefix = "User_";
        private const string AllUsersCacheKey = "AllUsers";
        private readonly TimeSpan _userCacheExpiration = TimeSpan.FromMinutes(5); 
        private readonly TimeSpan _allUsersCacheExpiration = TimeSpan.FromMinutes(10);

        public UserService(IReqResApiClient apiClient, ILogger<UserService> logger, IMemoryCache cache)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc />
        public async Task<User> GetUserById(int userId)
        {
            var cacheKey = $"{UserCacheKeyPrefix}{userId}";
            if (_cache.TryGetValue(cacheKey, out User user))
            {
                _logger.LogInformation("UserService: Retrieved user ID {UserId} from cache.", userId);
                return user;
            }

            _logger.LogInformation("UserService: Attempting to get user with ID {UserId} from API.", userId);
            user = await _apiClient.GetUserByIdAsync(userId);

            if (user != null)
            {
                _cache.Set(cacheKey, user, _userCacheExpiration);
                _logger.LogInformation("UserService: Successfully retrieved user with ID {UserId} from API and cached it.", userId);
            }
            else
            {
                _logger.LogWarning("UserService: User with ID {UserId} not found or API error occurred.", userId);
            }
            return user;
        }

        /// <inheritdoc />
        public async Task<List<User>> GetAllUsers()
        {
            if (_cache.TryGetValue(AllUsersCacheKey, out List<User> allUsers))
            {
                _logger.LogInformation("UserService: Retrieved all users from cache. Count: {Count}", allUsers.Count);
                return allUsers;
            }

            _logger.LogInformation("UserService: Attempting to get all users from API.");
            allUsers = await _apiClient.GetAllUsersAsync();

            if (allUsers != null && allUsers.Count > 0)
            {
                _cache.Set(AllUsersCacheKey, allUsers, _allUsersCacheExpiration);
                _logger.LogInformation("UserService: Retrieved {Count} total users from API and cached them.", allUsers.Count);
            }
            else
            {
                _logger.LogWarning("UserService: No users retrieved from API or an error occurred while fetching all users.");
                allUsers = new List<User>(); 
            }
            return allUsers;
        }
    }
}