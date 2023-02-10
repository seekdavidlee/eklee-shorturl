using System.Net.Http.Json;

namespace Eklee.ShortUrl.IntegrationTests;

[TestClass]
public class SmokeTests
{
    [TestMethod]
    public async Task CreateUpdateAndDelete()
    {
        var clientHandler = new HttpClientHandler { AllowAutoRedirect = false };
        using var httpClient = new HttpClient(clientHandler);
        httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("X_URL"));
        httpClient.DefaultRequestHeaders.Add("API_KEY", Environment.GetEnvironmentVariable("X_API_KEY"));

        var id = $"B1230";
        string url = $"https://{Guid.NewGuid():N}.com";

        var response = await httpClient.PostAsJsonAsync(id, new { url });
        response.EnsureSuccessStatusCode();

        var redirectResponse = await httpClient.GetAsync(id);

        Assert.AreEqual(System.Net.HttpStatusCode.Found, redirectResponse.StatusCode);
        Assert.IsNotNull(redirectResponse.Headers.Location);
        Assert.IsTrue(redirectResponse.Headers.Location.ToString().StartsWith(url));

        var deleteResponse = await httpClient.DeleteAsync(id);
        deleteResponse.EnsureSuccessStatusCode();

        redirectResponse = await httpClient.GetAsync(id);

        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, redirectResponse.StatusCode);
    }
}