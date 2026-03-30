using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace MyApp.Tests.E2E;

public sealed class BillingIngestionE2ETests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BillingIngestionE2ETests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task IngestRecord_NoToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new
        {
            provider   = "AWS",
            accountId  = "account-123",
            rawPayload = new { cost = 12.50 }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/billing/ingest", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

}
