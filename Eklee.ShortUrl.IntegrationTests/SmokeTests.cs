using System.Net;
using System.Net.Http.Json;

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
    private readonly HttpClientHandler clientHandler;

    public SmokeTests()
    {
        this.clientHandler = new HttpClientHandler { AllowAutoRedirect = false };
        this.httpClient = new HttpClient(clientHandler);

        var xurl = Environment.GetEnvironmentVariable("X_URL");

        if (string.IsNullOrEmpty(xurl))
        {
            throw new ArgumentNullException(nameof(xurl));
        }

        this.httpClient.BaseAddress = new Uri(xurl);

        var xapiKey = Environment.GetEnvironmentVariable("X_API_KEY");

        if (string.IsNullOrEmpty(xapiKey))
        {
            throw new ArgumentNullException(nameof(xapiKey));
        }

        httpClient.DefaultRequestHeaders.Add("API_KEY", xapiKey);
    }

    [TestMethod, TestCategory(Constants.Dev)]
    public async Task CreateUpdateAndDelete()
    {
        var id = $"B1230";
        string url = $"https://{Guid.NewGuid():N}.com";

        var response = await httpClient.PostAsJsonAsync(id, new { url });
        response.EnsureSuccessStatusCode();

        var redirectResponse = await httpClient.GetAsync(id);

        Assert.AreEqual(HttpStatusCode.Found, redirectResponse.StatusCode);
        Assert.IsNotNull(redirectResponse.Headers.Location);
        Assert.IsTrue(redirectResponse.Headers.Location.ToString().StartsWith(url));

        var deleteResponse = await httpClient.DeleteAsync(id);
        deleteResponse.EnsureSuccessStatusCode();

        redirectResponse = await httpClient.GetAsync(id);

        Assert.AreEqual(HttpStatusCode.NotFound, redirectResponse.StatusCode);
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

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }
}