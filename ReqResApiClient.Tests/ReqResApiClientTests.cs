using NUnit.Framework;
using ReqResApiClient.Clients;
using ReqResApiClient.Models;
using ReqResApiClient.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace ReqResApiClient.Tests
{
    public class ReqResApiClientTests
    {
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private Mock<ILogger<ReqResApiClient.Clients.ReqResApiClient>> _mockLogger;
        private ReqResApiClient.Clients.ReqResApiClient _apiClient;
        private IOptions<ReqResApiOptions> _options;

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<ReqResApiClient.Clients.ReqResApiClient>>();
            _options = Options.Create(new ReqResApiOptions { BaseUrl = "https://reqres.in/api/" });

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _apiClient = new ReqResApiClient.Clients.ReqResApiClient(httpClient, _mockLogger.Object, _options);
        }

        [TearDown]
        public void Teardown()
        {
            _mockHttpMessageHandler.VerifyAll();
        }

        private void SetupMockResponse(HttpStatusCode statusCode, string content, string requestUriPattern)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains(requestUriPattern)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response)
                .Verifiable();
        }


        [Test]
        public async Task GetUserByIdAsync_ReturnsUser_WhenSuccessful()
        {
            var user = new User { Id = 1, Email = "test@example.com", FirstName = "John", LastName = "Doe" };
            var responseData = new SingleUserResponse { Data = user, Support = new Support { Url = "url", Text = "text" } };
            SetupMockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(responseData), "users/1");

            var result = await _apiClient.GetUserByIdAsync(1);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Id);
            Assert.AreEqual("test@example.com", result.Email);
        }

        [Test]
        public async Task GetUserByIdAsync_ReturnsNull_WhenNotFound()
        {
            SetupMockResponse(HttpStatusCode.NotFound, "", "users/999");

            var result = await _apiClient.GetUserByIdAsync(999);

            Assert.IsNull(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP request failed")),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetUserByIdAsync_ReturnsNull_OnDeserializationError()
        {
            SetupMockResponse(HttpStatusCode.OK, "{ \"data\": { \"id\": \"not-an-int\" } }", "users/1");

            var result = await _apiClient.GetUserByIdAsync(1);

            Assert.IsNull(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("JSON deserialization failed")),
                    It.IsAny<JsonException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetUsersAsync_ReturnsUsers_WhenSuccessful()
        {
            var users = new List<User>
            {
                new User { Id = 1, Email = "user1@example.com" },
                new User { Id = 2, Email = "user2@example.com" }
            };
            var responseData = new UserListResponse { Page = 1, PerPage = 6, Total = 12, TotalPages = 2, Data = users };
            SetupMockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(responseData), "users?page=1");

            var result = await _apiClient.GetUsersAsync(1);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("user1@example.com", result[0].Email);
        }

        [Test]
        public async Task GetAllUsersAsync_FetchesAllPages()
        {
            var page1Users = new List<User>
            {
                new User { Id = 1, Email = "user1@example.com" },
                new User { Id = 2, Email = "user2@example.com" }
            };
            var page1Response = new UserListResponse { Page = 1, PerPage = 2, Total = 4, TotalPages = 2, Data = page1Users };
            var serializedPage1 = JsonSerializer.Serialize(page1Response);

            var page2Users = new List<User>
            {
                new User { Id = 3, Email = "user3@example.com" },
                new User { Id = 4, Email = "user4@example.com" }
            };
            var page2Response = new UserListResponse { Page = 2, PerPage = 2, Total = 4, TotalPages = 2, Data = page2Users };
            var serializedPage2 = JsonSerializer.Serialize(page2Response);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Returns<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
                {
                    if (request.RequestUri.ToString().Contains("users?page=1"))
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(serializedPage1) });
                    }
                    else if (request.RequestUri.ToString().Contains("users?page=2"))
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(serializedPage2) });
                    }
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent($"Unexpected URI: {request.RequestUri}") });
                })
                .Verifiable();


            var result = await _apiClient.GetAllUsersAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);
            Assert.That(result.Select(u => u.Id), Does.Contain(page1Users[0].Id));
            Assert.That(result.Select(u => u.Id), Does.Contain(page2Users[1].Id));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetching all users across all pages.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Information,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Finished fetching all users. Total users collected: 4")),
                   It.IsAny<Exception>(),
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        [Test]
        public async Task GetAllUsersAsync_HandlesHttpErrorGracefully()
        {
            _mockHttpMessageHandler.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("") })
               .Verifiable();

            var result = await _apiClient.GetAllUsersAsync();

            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP request failed while fetching all users")),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }

    public class UserServiceTests
    {
        private Mock<IReqResApiClient> _mockApiClient;
        private Mock<ILogger<UserService>> _mockLogger;
        private Mock<IMemoryCache> _mockCache;
        private UserService _userService;

        [SetUp]
        public void Setup()
        {
            _mockApiClient = new Mock<IReqResApiClient>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockCache = new Mock<IMemoryCache>();
            var mockCacheEntry = new Mock<ICacheEntry>();
            mockCacheEntry.SetupProperty(e => e.Value); 
            mockCacheEntry.SetupProperty(e => e.AbsoluteExpiration);
            mockCacheEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
            mockCacheEntry.SetupProperty(e => e.SlidingExpiration);
            mockCacheEntry.SetupProperty(e => e.Priority);
            mockCacheEntry.SetupProperty(e => e.Size);
            mockCacheEntry.SetupGet(e => e.Key).Returns(It.IsAny<object>());
            mockCacheEntry.SetupGet(e => e.PostEvictionCallbacks).Returns(new List<PostEvictionCallbackRegistration>());
            mockCacheEntry.Setup(e => e.Dispose()); 

            _mockCache
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(mockCacheEntry.Object);

            _userService = new UserService(_mockApiClient.Object, _mockLogger.Object, _mockCache.Object);
        }

        private void SetupCacheMiss<T>(string key)
        {
            object outValue = null; 
            _mockCache
                .Setup(m => m.TryGetValue(key, out outValue))
                .Returns(false);
        }

        private void SetupCacheHit<T>(string key, T cachedObject)
        {
            object outValue = cachedObject; 
            _mockCache
                .Setup(m => m.TryGetValue(key, out outValue))
                .Returns(true);
        }


        [Test]
        public async Task GetUserById_RetrievesFromApi_WhenCacheMiss_AndCaches()
        {
            SetupCacheMiss<User>("User_1"); 

            var expectedUser = new User { Id = 1, Email = "test@example.com" };
            _mockApiClient.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(expectedUser);

            var result = await _userService.GetUserById(1);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUser.Id, result.Id);

            _mockApiClient.Verify(x => x.GetUserByIdAsync(1), Times.Once); 
            _mockCache.Verify(m => m.CreateEntry(It.Is<object>(key => (string)key == "User_1")), Times.Once); 
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("UserService: Successfully retrieved user with ID 1 from API and cached it.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetUserById_RetrievesFromCache_WhenCacheHit()
        {
            var cachedUser = new User { Id = 1, Email = "cached@example.com" };
            SetupCacheHit("User_1", cachedUser);

            var result = await _userService.GetUserById(1);

            Assert.IsNotNull(result);
            Assert.AreEqual(cachedUser.Id, result.Id);
            Assert.AreEqual(cachedUser.Email, result.Email);

            _mockApiClient.Verify(x => x.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
            _mockCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Never);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("UserService: Retrieved user ID 1 from cache.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetUserById_ReturnsNull_WhenApiReturnsNull()
        {
            SetupCacheMiss<User>("User_999"); 
            _mockApiClient.Setup(x => x.GetUserByIdAsync(999)).ReturnsAsync((User)null);

            var result = await _userService.GetUserById(999);

            Assert.IsNull(result);

            _mockApiClient.Verify(x => x.GetUserByIdAsync(999), Times.Once);
            _mockCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Never);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("UserService: User with ID 999 not found or API error occurred.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetAllUsers_RetrievesFromApi_WhenCacheMiss_AndCaches()
        {
            SetupCacheMiss<List<User>>("AllUsers"); 
            var expectedUsers = new List<User> { new User { Id = 1 }, new User { Id = 2 } };
            _mockApiClient.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(expectedUsers);

            var result = await _userService.GetAllUsers();

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);

            _mockApiClient.Verify(x => x.GetAllUsersAsync(), Times.Once); 
            _mockCache.Verify(m => m.CreateEntry(It.Is<object>(key => (string)key == "AllUsers")), Times.Once); 
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("UserService: Retrieved 2 total users from API and cached them.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetAllUsers_RetrievesFromCache_WhenCacheHit()
        {
            var cachedUsers = new List<User> { new User { Id = 100 }, new User { Id = 101 } };
            SetupCacheHit("AllUsers", cachedUsers); 

            var result = await _userService.GetAllUsers();

            Assert.IsNotNull(result);
            Assert.AreEqual(cachedUsers.Count, result.Count);
            Assert.AreEqual(cachedUsers[0].Id, result[0].Id);

            _mockApiClient.Verify(x => x.GetAllUsersAsync(), Times.Never);
            _mockCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Never); 
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"UserService: Retrieved all users from cache. Count: {cachedUsers.Count}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetAllUsers_ReturnsEmptyList_OnError()
        {
            SetupCacheMiss<List<User>>("AllUsers"); 
            _mockApiClient.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(new List<User>());

            var result = await _userService.GetAllUsers();

            Assert.IsNotNull(result);
            Assert.IsEmpty(result);

            _mockApiClient.Verify(x => x.GetAllUsersAsync(), Times.Once); 
            _mockCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Never); 
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("UserService: No users retrieved from API or an error occurred while fetching all users.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
