using ReqResApiClient.Clients;
using ReqResApiClient.Extensions;
using ReqResApiClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ReqResApiClient.ConsoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var userService = host.Services.GetRequiredService<IUserService>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("--- ReqRes API Client Demo ---");
            Console.WriteLine("\nFetching user with ID 2...");
            var user = await userService.GetUserById(2);
            if (user != null)
            {
                Console.WriteLine($"  User Found: {user.FirstName} {user.LastName} ({user.Email})");
            }
            else
            {
                Console.WriteLine("  User with ID 2 not found or an error occurred.");
            }

            Console.WriteLine("\nFetching non-existent user with ID 999...");
            var nonExistentUser = await userService.GetUserById(999);
            if (nonExistentUser != null)
            {
                Console.WriteLine($"  User Found (Error!): {nonExistentUser.FirstName} {nonExistentUser.LastName}");
            }
            else
            {
                Console.WriteLine("  User with ID 999 not found (as expected).");
            }

            Console.WriteLine("\nFetching all users...");
            var allUsers = await userService.GetAllUsers();
            if (allUsers != null && allUsers.Count > 0)
            {
                Console.WriteLine($"  Total users retrieved: {allUsers.Count}");
                foreach (var u in allUsers)
                {
                    Console.WriteLine($"    - ID: {u.Id}, Name: {u.LastName}, Email: {u.Email}");
                }
            }
            else
            {
                Console.WriteLine("  No users retrieved or an error occurred while fetching all users.");
            }

            Console.WriteLine("\n--- Demo Complete ---");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(configure => configure.AddConsole());
                    services.AddReqResApiClientServices(options =>
                    {
                        hostContext.Configuration.GetSection(ReqResApiOptions.ReqResApi).Bind(options);
                    });
                });
    }
}
