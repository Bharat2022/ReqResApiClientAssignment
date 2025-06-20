using ReqResApiClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;


namespace ReqResApiClient.Clients
{
    public class ReqResApiOptions
    {
        public const string ReqResApi = "ReqResApi"; 
        public string BaseUrl { get; set; } = "https://reqres.in/api/"; 
    }

    public class ReqResApiClient : IReqResApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReqResApiClient> _logger;
        private readonly string _baseUrl;

        public ReqResApiClient(HttpClient httpClient, ILogger<ReqResApiClient> logger, IOptions<ReqResApiOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _baseUrl = options?.Value?.BaseUrl ?? throw new ArgumentNullException(nameof(options));

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
            }
        }

        /// <inheritdoc />
        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Fetching user with ID: {UserId}", userId);
                var response = await _httpClient.GetAsync($"users/{userId}");
                response.EnsureSuccessStatusCode(); 

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var singleUserResponse = JsonSerializer.Deserialize<SingleUserResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return singleUserResponse?.Data;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request failed for user ID {UserId}: {StatusCode}", userId, httpEx.StatusCode);
                return null;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization failed for user ID {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching user ID {UserId}", userId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<List<User>> GetUsersAsync(int page = 1)
        {
            if (page < 1) page = 1; 

            try
            {
                _logger.LogInformation("Fetching users on page: {Page}", page);
                var response = await _httpClient.GetAsync($"users?page={page}");
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var userListResponse = JsonSerializer.Deserialize<UserListResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return userListResponse?.Data ?? new List<User>();
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request failed for users page {Page}: {StatusCode}", page, httpEx.StatusCode);
                return new List<User>();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization failed for users page {Page}", page);
                return new List<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching users page {Page}", page);
                return new List<User>();
            }
        }

        /// <inheritdoc />
        public async Task<List<User>> GetAllUsersAsync()
        {
            _logger.LogInformation("Fetching all users across all pages.");
            var allUsers = new List<User>();
            int currentPage = 1;
            int totalPages = 1; 

            while (currentPage <= totalPages)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"users?page={currentPage}");
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var userListResponse = JsonSerializer.Deserialize<UserListResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (userListResponse != null && userListResponse.Data != null)
                    {
                        allUsers.AddRange(userListResponse.Data);
                        totalPages = userListResponse.TotalPages; 
                        _logger.LogInformation("Fetched page {CurrentPage}/{TotalPages}. Users found: {Count}", currentPage, totalPages, userListResponse.Data.Count);
                    }
                    else
                    {
                        _logger.LogWarning("API response for page {CurrentPage} was null or contained no data.", currentPage);
                        break; 
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, "HTTP request failed while fetching all users on page {Page}: {StatusCode}", currentPage, httpEx.StatusCode);
                    break; 
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON deserialization failed while fetching all users on page {Page}", currentPage);
                    break; 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred while fetching all users on page {Page}", currentPage);
                    break; 
                }

                currentPage++;
            }
            _logger.LogInformation("Finished fetching all users. Total users collected: {TotalUsers}", allUsers.Count);
            return allUsers;
        }
    }
}