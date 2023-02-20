using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Eklee.ShortUrl.IntegrationTests;

public static class Constants
{
    public const string Dev = "dev";
    public const string Prod = "prod";
}

[TestClass]
public class SmokeTests : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly HttpClient unauthenticatedHttpClient;
    private readonly HttpClientHandler clientHandler;

    public SmokeTests()
    {

        this.clientHandler = new HttpClientHandler { AllowAutoRedirect = false };
        this.httpClient = HttpClientFactory.Create(clientHandler);
        this.unauthenticatedHttpClient = HttpClientFactory.Create();

        var xurl = Environment.GetEnvironmentVariable("X_URL");

        if (string.IsNullOrEmpty(xurl))
        {
            throw new ArgumentNullException(nameof(xurl));
        }

        this.httpClient.BaseAddress = new Uri(xurl);
        this.unauthenticatedHttpClient.BaseAddress = new Uri(xurl);

        var xapiKey = Environment.GetEnvironmentVariable("X_API_KEY");

        if (string.IsNullOrEmpty(xapiKey))
        {
            throw new ArgumentNullException(nameof(xapiKey));
        }

        httpClient.DefaultRequestHeaders.Add("API_KEY", xapiKey);
    }

    [TestMethod, TestCategory(Constants.Dev)]
    public async Task CreateAndGetUnauthorized()
    {
        var id = Guid.NewGuid().ToString("N");
        string url = $"https://{Guid.NewGuid():N}.com";

        var response = await unauthenticatedHttpClient.PostAsJsonAsync(id, new { url });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod, TestCategory(Constants.Dev)]
    public async Task CreateUpdateAndDelete()
    {
        var id = Guid.NewGuid().ToString("N");
        string url = $"https://{Guid.NewGuid():N}.com";

        var response = await httpClient.PostAsJsonAsync(id, new { url });
        response.EnsureSuccessStatusCode();

        var redirectResponse = await httpClient.GetAsync(id);

        Assert.AreEqual(HttpStatusCode.Found, redirectResponse.StatusCode);
        Assert.IsNotNull(redirectResponse.Headers.Location);
        Assert.IsTrue(redirectResponse.Headers.Location.ToString().StartsWith(url));

        // Get a second time
        await httpClient.GetAsync(id);

        int year = DateTime.UtcNow.Year;
        var statResponse = await httpClient.GetAsync($"stats/{year}");
        statResponse.EnsureSuccessStatusCode();

        var dto = JsonSerializer.Deserialize<Stats>(await statResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.IsNotNull(dto);
        Assert.AreEqual(year, dto.Year);

        var thisStat = dto.VisitStats.SingleOrDefault(x => x.Key == id);
        Assert.IsNotNull(thisStat);
        Assert.AreEqual(2, thisStat.VisitCount);

        var deleteResponse = await httpClient.DeleteAsync(id);
        deleteResponse.EnsureSuccessStatusCode();

        redirectResponse = await httpClient.GetAsync(id);

        Assert.AreEqual(HttpStatusCode.NotFound, redirectResponse.StatusCode);
    }

    [TestMethod, TestCategory(Constants.Dev), TestCategory(Constants.Prod)]
    public async Task GetStatsWithBadYear_GetBadRequest()
    {
        int year = DateTime.UtcNow.Year - 6;
        var response = await httpClient.GetAsync($"stats/{year}");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod, TestCategory(Constants.Dev), TestCategory(Constants.Prod)]
    public async Task GetSwaggerUI()
    {
        this.clientHandler.AllowAutoRedirect = true;

        var response = await httpClient.GetAsync("swagger");
        response.EnsureSuccessStatusCode();
        Assert.IsNotNull(response.RequestMessage);
        Assert.IsNotNull(response.RequestMessage.RequestUri);
        Assert.IsTrue(response.RequestMessage.RequestUri.ToString().EndsWith("swagger/ui"));
    }

    [TestMethod, TestCategory(Constants.Dev), TestCategory(Constants.Prod)]
    public async Task GetOpenAPIJason()
    {
        this.clientHandler.AllowAutoRedirect = true;

        var response = await httpClient.GetAsync("swagger.json");
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        this.httpClient.Dispose();
        this.unauthenticatedHttpClient.Dispose();

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }
}