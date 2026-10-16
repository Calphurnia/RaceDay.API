using System.Net;
using System.Net.Http.Json;
using Xunit;
namespace RaceDay.API.Tests;
public class ApiEndpointTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public ApiEndpointTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    // Test 1: The public events endpoint should work
    // without requiring a login.
    [Fact]
    public async Task GetEvents_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    // Test 2: A request for an event that does not
    // exist should return 404.
    [Fact]
    public async Task GetMissingEvent_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    // Test 3: Creating an event without a JWT token
    // should be rejected.
    [Fact]
    public async Task CreateEventWithoutToken_ReturnsUnauthorized()
    {
        var eventRequest = new
        {
            name = "Automated Test Race",
            description = "Event created during testing",
            eventDate = DateTime.UtcNow.AddDays(30),
            location = "Pretoria",
            distance = 5.0,
            eventType = "running"
        };
        var response = await _client.PostAsJsonAsync(
            "/api/events",
            eventRequest);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    // Test 4: The public categories endpoint should
    // return 404 when its event does not exist.
    [Fact]
    public async Task GetCategoriesForMissingEvent_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            "/api/events/999999/categories");
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}