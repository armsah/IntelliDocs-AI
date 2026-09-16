using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntelliDocs.IntegrationTests.Api;

public sealed class HealthEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(
        WebApplicationFactory<Program> factory)
    {
        _factory =
            factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting(
                    "ConnectionStrings:PostgreSql",
                    "Host=127.0.0.1;Port=5433;" +
                    "Database=intellidocs;" +
                    "Username=intellidocs;" +
                    "Password=intellidocs_dev");

                builder.UseSetting(
                    "ConnectionStrings:BlobStorage",
                    "UseDevelopmentStorage=true");

                builder.UseSetting(
                    "BlobStorage:ContainerName",
                    "documents");
            });
    }

    [Fact]
    public async Task Health_DoesNotRequireEntraConfiguration()
    {
        using var client = _factory.CreateClient();

        using var response =
            await client.GetAsync("/health");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "\"status\":\"healthy\"",
            body);

        Assert.Contains(
            "\"service\":\"IntelliDocs.Api\"",
            body);
    }
}