using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using aspnetproject.Infrastructure.Dtos.Auth;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.IntegrationTests.OpenApi;

// The OpenAPI documents are only mapped in Development, so these tests run the host in that environment.
// They never touch the database, so no reseed is needed.
[Collection("Database")]
public class OpenApiDocumentTests : IAsyncLifetime
{
    private const string OpenApiDocumentUrl = "openapi/v1.json";
    private const string SwaggerDocumentUrl = "swagger/v1/swagger.json";
    private const string DateOfBirthProperty = "dateOfBirth";
    private const string ComponentsProperty  = "components";
    private const string SchemasProperty     = "schemas";
    private const string PropertiesProperty  = "properties";
    private const string DefaultProperty     = "default";
    private const string UtcSuffix           = "Z";

    // CreateAccountDto is not used by any endpoint yet, so it is in neither document.
    private static readonly (string Name, Type BodyType)[] PrefilledRequestBodies =
    [
        (nameof(RegisterDto), typeof(RegisterDto)),
        (nameof(EditUserDto), typeof(EditUserDto))
    ];

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OpenApiDocumentTests()
    {
        _factory = new CustomWebApplicationFactory(environmentName: CustomWebApplicationFactory.DevelopmentEnvironment);
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetOpenApiDocument_InDevelopment_ReturnsDocumentWithPrefilledRequestBodies()
    {
        var response = await _client.GetAsync(OpenApiDocumentUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var schemas = document.RootElement.GetProperty(ComponentsProperty).GetProperty(SchemasProperty);

        foreach (var (name, _) in PrefilledRequestBodies)
        {
            Assert.True(schemas.TryGetProperty(name, out _), $"{name} is missing from the OpenAPI document.");
        }
    }

    [Fact]
    public async Task GetSwaggerDocument_InDevelopment_PrefillsBodiesThatPassValidationWithDateOfBirthInUtc()
    {
        var response = await _client.GetAsync(SwaggerDocumentUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var schemas = document.RootElement.GetProperty(ComponentsProperty).GetProperty(SchemasProperty);

        foreach (var (name, bodyType) in PrefilledRequestBodies)
        {
            var properties = schemas.GetProperty(name).GetProperty(PropertiesProperty);

            // Swagger UI prefills each field with its schema default, so rebuild the body the same way.
            var prefilledBody = new Dictionary<string, JsonElement>();
            foreach (var property in properties.EnumerateObject())
            {
                if (property.Value.TryGetProperty(DefaultProperty, out var prefilledValue))
                {
                    prefilledBody[property.Name] = prefilledValue;
                }
            }

            var prefilledDateOfBirth = prefilledBody[DateOfBirthProperty].GetString()!;
            Assert.EndsWith(UtcSuffix, prefilledDateOfBirth);

            var body = JsonSerializer.Deserialize(JsonSerializer.Serialize(prefilledBody), bodyType, JsonSerializerOptions.Web)!;
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(body, new ValidationContext(body), validationResults, validateAllProperties: true);

            Assert.True(isValid, $"{name}: {string.Join("; ", validationResults.Select(r => r.ErrorMessage))}");
        }
    }
}
