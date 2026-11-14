using aspnetproject.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace aspnetproject.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Replaces the first entry of Cors:AllowedOrigins, so tests that need a trusted origin do not depend on the settings file.
    public const string TrustedFrontendOrigin = "http://frontend.webcity.test:4200";

    // The host's own switch for reloadOnChange on the default appsettings*.json sources. Those sources are built before
    // ConfigureAppConfiguration runs, so flipping ReloadOnChange on them there has no effect. Each file watcher holds an inotify
    // instance, and Linux caps them per user (128 in the cloud container).
    private const string ReloadConfigOnChangeSetting = "hostBuilder:reloadConfigOnChange";
    private const string DisabledSettingValue        = "false";

    private const string UploadFolderSetting      = "ImageStorage:UploadFolder";
    private const string TestUploadFolderRootName = "webcity-test-uploads";

    public const string TestingEnvironment     = "Testing";
    public const string DevelopmentEnvironment = "Development";

    private readonly IReadOnlyDictionary<string, string?> _settingOverrides;
    private readonly string _environmentName;

    // Each host stores uploads in its own temporary folder, removed when the host is disposed.
    public string UploadFolder { get; } =
        Path.Combine(Path.GetTempPath(), TestUploadFolderRootName, Guid.NewGuid().ToString("N"));

    // settingOverrides win over the settings file and the suite's defaults, e.g. a lower ImageStorage:MaxFileSizeBytes.
    // environmentName is Testing unless a test needs a Development-only feature, such as the OpenAPI document.
    public CustomWebApplicationFactory(IReadOnlyDictionary<string, string?>? settingOverrides = null, string environmentName = TestingEnvironment)
    {
        _settingOverrides = settingOverrides ?? new Dictionary<string, string?>();
        _environmentName = environmentName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        builder.UseSetting(ReloadConfigOnChangeSetting, DisabledSettingValue);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.Testing.json", optional: false);

            // Added after the settings file so it wins: the suite never sends real email, and always trusts TrustedFrontendOrigin.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailConfiguration:Enabled"] = "false",
                ["Cors:AllowedOrigins:0"] = TrustedFrontendOrigin,
                [UploadFolderSetting] = UploadFolder
            });

            config.AddInMemoryCollection(_settingOverrides);
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
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        DeleteUploadFolder();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DeleteUploadFolder();
    }

    private void DeleteUploadFolder()
    {
        bool folderExists = Directory.Exists(UploadFolder);
        if (folderExists)
        {
            Directory.Delete(UploadFolder, recursive: true);
        }
    }
}
