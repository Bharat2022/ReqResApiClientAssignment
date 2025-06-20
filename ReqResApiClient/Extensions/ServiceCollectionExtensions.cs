using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using ReqResApiClient.Clients;
using ReqResApiClient.Services;
using System.Net; 

namespace ReqResApiClient.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds ReqRes API client, User Service, Caching, and Polly Retry to the IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to.</param>
        /// <param name="configureOptions">Action to configure the ReqResApiOptions.</param>
        /// <returns>The updated IServiceCollection.</returns>
        public static IServiceCollection AddReqResApiClientServices(this IServiceCollection services, Action<ReqResApiOptions> configureOptions)
        {
            services.Configure(configureOptions);
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError() 
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        Console.WriteLine($"Polly: Retrying HTTP request (attempt {retryAttempt}). Waiting {timespan.TotalSeconds} seconds. Status: {outcome.Result?.StatusCode ?? HttpStatusCode.OK}");
                    }
                );

            services.AddHttpClient<IReqResApiClient, Clients.ReqResApiClient>()
                    .AddPolicyHandler(retryPolicy); 
            services.AddMemoryCache(); 
            services.AddTransient<IUserService, UserService>();
            return services;
        }
    }
}