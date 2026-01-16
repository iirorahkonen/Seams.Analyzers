# SEAM016: Direct HttpClient Creation

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM016 |
| **Category** | Infrastructure |
| **Severity** | Warning |
| **Enabled** | Yes |

## Description

Detects direct instantiation of `HttpClient` using `new HttpClient()`. Direct creation can lead to socket exhaustion and makes testing difficult.

## Why This Is Problematic

Creating `HttpClient` directly causes multiple issues:

1. **Socket Exhaustion**: Each `HttpClient` instance holds onto connections. Creating many instances exhausts available sockets, causing `SocketException` errors
2. **DNS Changes Ignored**: `HttpClient` caches DNS lookups. Long-lived instances won't pick up DNS changes
3. **Difficult to Mock**: Direct instantiation couples code to actual HTTP calls, making unit testing require real network access
4. **No Centralized Configuration**: Each instance must be configured separately (timeouts, headers, handlers)
5. **Resource Leaks**: Improper disposal can leak resources, while disposing too eagerly causes socket exhaustion

## Examples

### Non-Compliant Code

```csharp
public class WeatherService
{
    public async Task<Weather> GetWeatherAsync(string city)
    {
        // Bad: Creating new HttpClient for each request
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.weather.com");
        client.DefaultRequestHeaders.Add("Api-Key", "my-key");

        var response = await client.GetAsync($"/weather/{city}");
        return await response.Content.ReadFromJsonAsync<Weather>();
    }
}
```

```csharp
public class ApiClient
{
    // Bad: Static HttpClient (solves socket exhaustion but still has issues)
    private static readonly HttpClient _client = new HttpClient();

    public async Task<string> GetDataAsync(string url)
    {
        return await _client.GetStringAsync(url);
    }
}
```

```csharp
public class PaymentGateway
{
    private readonly HttpClient _client;

    public PaymentGateway()
    {
        // Bad: Creating HttpClient in constructor
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://payments.example.com"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}
```

### Compliant Code

```csharp
public class WeatherService : IWeatherService
{
    private readonly HttpClient _client;

    // Good: HttpClient injected via IHttpClientFactory
    public WeatherService(HttpClient client)
    {
        _client = client;
    }

    public async Task<Weather> GetWeatherAsync(string city)
    {
        var response = await _client.GetAsync($"/weather/{city}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Weather>();
    }
}

// Registration in Startup/Program.cs
services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
    client.BaseAddress = new Uri("https://api.weather.com");
    client.DefaultRequestHeaders.Add("Api-Key", "my-key");
});
```

```csharp
public class ApiClient : IApiClient
{
    private readonly IHttpClientFactory _clientFactory;

    // Good: Using IHttpClientFactory
    public ApiClient(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<string> GetDataAsync(string url)
    {
        using var client = _clientFactory.CreateClient("api");
        return await client.GetStringAsync(url);
    }
}

// Registration
services.AddHttpClient("api", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

```csharp
// Good: For testing, use MockHttp or similar
public class WeatherServiceTests
{
    [Fact]
    public async Task GetWeatherAsync_ReturnsWeather()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://api.weather.com/weather/London")
            .Respond("application/json", "{\"temp\": 20}");

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://api.weather.com");

        var service = new WeatherService(client);

        // Act
        var result = await service.GetWeatherAsync("London");

        // Assert
        Assert.Equal(20, result.Temp);
    }
}
```

## How to Fix

1. **Add IHttpClientFactory**: Register HTTP client services in your DI container
2. **Use Typed Clients**: Create typed HTTP client classes that receive `HttpClient` via constructor
3. **Configure in Registration**: Set base address, headers, and handlers during service registration
4. **Inject HttpClient**: Let the DI container manage `HttpClient` lifetime
5. **Use Named Clients**: For multiple HTTP endpoints, use named client registration

### Migration Steps

```csharp
// Step 1: Add HttpClient services to DI
// In Program.cs or Startup.cs
services.AddHttpClient();

// Step 2: For typed clients
services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
    client.BaseAddress = new Uri(Configuration["Weather:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy()); // Optional: Add Polly policies

// Step 3: Inject HttpClient in constructor
public class WeatherService : IWeatherService
{
    private readonly HttpClient _client;

    public WeatherService(HttpClient client)
    {
        _client = client;
    }
}
```

## When to Suppress

Suppression is appropriate when:

- You're implementing `IHttpClientFactory` or similar infrastructure
- You're in **test code** creating clients for test servers
- You're working with a **static readonly field** pattern (analyzer skips these)
- The code is in a **console application** or short-lived process where socket exhaustion isn't a concern

```csharp
#pragma warning disable SEAM016
// Test helper that creates clients for integration tests
public static HttpClient CreateTestClient(TestServer server)
{
    return server.CreateClient();
}
#pragma warning restore SEAM016
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM016.severity = none

# Or set to suggestion instead of warning
dotnet_diagnostic.SEAM016.severity = suggestion
```

## Related Rules

- [SEAM001](SEAM001.md) - Direct Instantiation (general new keyword issues)
- [SEAM015](SEAM015.md) - File System Access (similar infrastructure dependency)
- [SEAM017](SEAM017.md) - Database Access (similar infrastructure concern)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Use IHttpClientFactory to implement resilient HTTP requests](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
- [You're using HttpClient wrong](https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/) by ASP.NET Monsters
- [HttpClientFactory in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests)
