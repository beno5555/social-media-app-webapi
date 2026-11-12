using aspnetproject.Data;
using aspnetproject.Hubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace aspnetproject.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Replaces the first entry of Cors:AllowedOrigins, so tests that need a trusted origin do not depend on the settings file.
    public const string TrustedFrontendOrigin = "http://frontend.webcity.test:4200";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.Testing.json", optional: false);

            // Added after the settings file so it wins: the suite never sends real email, and always trusts TrustedFrontendOrigin.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailConfiguration:Enabled"] = "false",
                ["Cors:AllowedOrigins:0"] = TrustedFrontendOrigin
            });
        });

        builder.ConfigureServices(services =>
        {
            var hostedServices = services
                .Where(s => s.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var serviceDescriptor in hostedServices)
            {
                services.Remove(serviceDescriptor);
            }

            // services.RemoveAll<IHubContext<MessageHub>>();
            // services.AddSingleton(Mock.Of<IHubContext<MessageHub>>());
        });
    }
}