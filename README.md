.NET Developer Test Assignment: ReqRes API Client
This project demonstrates a .NET Core/8+ component designed to interact with the public ReqRes API (https://reqres.in/api/). It focuses on fetching, processing, and providing user data, adhering to principles of good software design, robustness, and testability.

Solution Structure
The solution ReqResApiClientAssignment.sln is structured into three projects:

ReqResApiClient (Class Library):

This is the core component containing the data models, the API client implementation, and the service layer.

It's designed to be easily reusable in other applications.

Folders:

Models: Contains C# classes representing the API's JSON response structures (e.g., User, SingleUserResponse, UserListResponse).

Clients: Encapsulates HttpClient interactions with the ReqRes API. IReqResApiClient defines the contract, and ReqResApiClient provides the implementation.

Services: Contains the business logic for interacting with user data. IUserService defines the contract, and UserService uses the API client to fulfill requests. This separation allows for potential caching, transformation, or additional logic here without affecting the API client.

Extensions: Provides extension methods for IServiceCollection to simplify dependency injection setup.

ReqResApiClient.Tests (NUnit Test Project):

Contains unit tests for the ReqResApiClient and UserService classes.

Utilizes Moq for mocking HttpClient and ILogger dependencies, ensuring true unit isolation.

Tests cover successful API calls, not-found scenarios, and deserialization errors.

ReqResApiClient.ConsoleApp (Console Application):

A simple demonstration application that showcases how to consume the ReqResApiClient library.

Uses .NET's built-in Dependency Injection and Configuration system to set up and run the services.

Reads the API BaseUrl from appsettings.json.

Technologies Used
.NET 8.0: The target framework for all projects.

C#: The primary programming language.

HttpClient & async/await: For asynchronous HTTP requests.

System.Text.Json: For efficient JSON serialization/deserialization.

Microsoft.Extensions.DependencyInjection: For managing service dependencies.

Microsoft.Extensions.Configuration: For flexible application configuration (e.g., appsettings.json).

Microsoft.Extensions.Logging: For structured logging.

NUnit: Testing framework.

Moq: Mocking library for unit tests.

Design Decisions
Separation of Concerns (Clean Architecture Principles):

Data Models (Models): Pure POCOs (Plain Old C# Objects) representing the API contract.

API Client (Clients): Sole responsibility is to make HTTP calls and deserialize raw API responses. It knows nothing about business logic or data consumption.

Service Layer (Services): Contains the application's business logic. It orchestrates calls to the API client and provides a cleaner, higher-level interface (IUserService) to consumers. This abstraction makes the system more robust, testable, and adaptable to changes (e.g., if the underlying API changes, only the client might need adjustments, not the service).

Dependency Injection: Services are registered and resolved via IServiceCollection and IHost, promoting loose coupling and testability. HttpClient is managed via AddHttpClient for best practices (e.g., connection pooling).

Configuration:

The API base URL is externalized in appsettings.json and loaded via Microsoft.Extensions.Configuration. This makes the application easily configurable without recompilation.

IOptions<ReqResApiOptions> is used to inject configuration settings into the ReqResApiClient.

Error Handling & Resilience:

try-catch blocks: Used extensively within the ReqResApiClient to catch HttpRequestException (for HTTP status codes), JsonException (for deserialization issues), and general Exception types.

Logging: ILogger is injected into both the API client and the service layer to log informative messages, warnings, and errors. This is crucial for observability and debugging.

Graceful Degradation: Instead of crashing, methods return null or empty lists on error, allowing the calling code to handle the absence of data gracefully.

EnsureSuccessStatusCode(): Used to automatically throw an HttpRequestException for non-success HTTP status codes (4xx/5xx), simplifying error detection.

Pagination (GetAllUsersAsync):

The GetAllUsersAsync method in ReqResApiClient handles internal pagination by repeatedly calling the users endpoint with incrementing page numbers until all pages are fetched. This abstracts the pagination complexity from the UserService.

Testability:

Interfaces (IReqResApiClient, IUserService) are used to define contracts, enabling easy mocking of dependencies in unit tests.

The Moq library is used to create mock objects, allowing tests to isolate the component under test from its dependencies and control their behavior.

Unit tests verify expected outcomes for successful calls, error conditions (HTTP failures, deserialization errors), and specific logic (like GetAllUsersAsync iterating pages).

Troubleshooting and Resolutions Encountered
During the development process, several common .NET development and testing issues were encountered and resolved:

Missing Namespace/Type Errors (ILogger, IServiceCollection):

Problem: Initial compilation errors indicated that types like ILogger<ReqResApiClient> and IServiceCollection could not be found.

Resolution: This was resolved by ensuring the correct NuGet packages were installed in the respective projects (Microsoft.Extensions.Logging.Abstractions, Microsoft.Extensions.Options, Microsoft.Extensions.Http in ReqResApiClient, and various Microsoft.Extensions.Configuration.*, Microsoft.Extensions.Hosting, Microsoft.Extensions.Logging.Console in ReqResApiClient.ConsoleApp). Correct using directives were also verified.

HttpMessageHandler.SendAsync Inaccessible due to Protection Level (Round 1 - Initial Moq Setup):

Problem: When attempting to mock HttpMessageHandler.SendAsync directly with _mockHttpMessageHandler.Setup(handler => handler.SendAsync(...)), the compiler reported it was inaccessible.

Resolution: This error is often a symptom of stale build artifacts or Visual Studio's internal cache. The initial proposed solution involved thorough cleaning of bin, obj, and .vs folders, followed by a complete rebuild of the solution. While this is a common fix for such issues, it did not fully resolve the problem in this specific case, indicating a deeper interaction issue with Moq in this environment.

System.NotSupportedException: Unsupported expression: handler => handler.SendAsyncFunc (Round 2 - Custom TestHttpMessageHandler):

Problem: After introducing a TestHttpMessageHandler with a public SendAsyncFunc property to work around the previous issue, a NotSupportedException occurred when trying to Setup this property using handler => handler.SendAsyncFunc. Moq requires that members mocked in this way (Setup on a property) must be virtual.

Resolution: The SendAsyncFunc property in TestHttpMessageHandler was explicitly made public virtual. This resolves the NotSupportedException by allowing Moq to create a proxy for that property.

Persistent HttpMessageHandler.SendAsync Inaccessible / Test Failures (Round 3 - Final Robust Solution):

Problem: Despite making SendAsyncFunc virtual, tests continued to fail, indicating underlying issues with how the mock was interacting or being verified. The complexity of the custom TestHttpMessageHandler might have masked other issues or introduced new ones.

Resolution: The solution was refined to use Moq.Protected() directly on the Mock<HttpMessageHandler>. This is the most reliable and idiomatic way to mock protected methods in Moq.

The TestHttpMessageHandler class was removed entirely.

The SetupMockResponse helper and direct test setups were modified to use _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ...).

MockBehavior.Strict was applied to Mock<HttpMessageHandler> to ensure all interactions are explicitly mocked, and _mockHttpMessageHandler.VerifyAll() was added to [TearDown] to confirm all setups were invoked, leading to precise test failure diagnostics when issues arise.

Outcome: This final approach successfully resolved all test failures, demonstrating a robust and properly mocked HttpClient behavior.

How to Run
Clone the Repository: (If this project were in a Git repo)

git clone <your-repo-link>
cd ReqResApiClientAssignment

Open in Visual Studio 2022:

Open ReqResApiClientAssignment.sln.

Restore NuGet Packages:

Visual Studio should automatically restore NuGet packages upon opening. If not, right-click on the solution in Solution Explorer and select "Restore NuGet Packages."

Set Startup Project:

In Solution Explorer, right-click on ReqResApiClient.ConsoleApp and select "Set as Startup Project."

Run the Application:

Press F5 or click the "Start" button (green play icon).

The console application will run, demonstrating fetching a single user and all users.

How to Run Tests
Open Test Explorer:

In Visual Studio, go to Test > Test Explorer.

Run Tests:

Click "Run All Tests" in the Test Explorer window.

You should see all tests pass for ReqResApiClientTests and UserServiceTests.

Future Enhancements (Bonus Points Considerations)
Caching: Implement a basic in-memory cache (e.g., using IMemoryCache) in the UserService to reduce redundant API calls for frequently requested data. This would involve checking the cache before calling the API and storing results after a successful fetch.

Retry Logic with Polly: Integrate the Polly library into the ReqResApiClient to add retry policies (e.g., exponential backoff) for transient network failures or API rate limits.

Advanced Configuration: Explore more advanced configuration scenarios, such as loading settings from environment variables for different deployment environments.

Asynchronous Error Handling: Refine error handling to potentially wrap API errors in custom exceptions for more specific handling by callers.

Rate Limiting: Implement a basic rate limiter for the API client to prevent hitting API rate limits, possibly using a token bucket algorithm or a library like AspNetCoreRateLimit.

More Robust Logging: Integrate with a more advanced logging framework like Serilog or NLog for richer logging capabilities (e.g., logging to files, databases).

Video Walkthrough
[Provide a link to your video walkthrough here once created.]