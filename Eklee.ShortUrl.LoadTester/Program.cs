using NBomber.Contracts;
using NBomber.CSharp;
using System.Net.Http.Json;

int rate = int.Parse(Environment.GetEnvironmentVariable("X_RATE"));
int intervalInSeconds = int.Parse(Environment.GetEnvironmentVariable("X_INTERVAL_IN_SECONDS"));
int durationInSeconds = int.Parse(Environment.GetEnvironmentVariable("X_DURATION_IN_MINS")) * 60;

var clientHandler = new HttpClientHandler { AllowAutoRedirect = false };
using var httpClient = new HttpClient(clientHandler);
httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("X_URL"));
httpClient.DefaultRequestHeaders.Add("API_KEY", Environment.GetEnvironmentVariable("X_API_KEY"));

ScenarioProps[] list = new ScenarioProps[3];
for (int i = 0; i < list.Length; i++)
{
    var id = $"A123{i}";
    string url = $"https://{Guid.NewGuid():N}.com";

    var getRedirects = Scenario.Create($"get_redirects_{i}", async context =>
    {
        try
        {
            var response = await httpClient.GetAsync(id);
            if (response.StatusCode.Equals(System.Net.HttpStatusCode.Found) &&
                response.Headers.Location != null &&
                response.Headers.Location.ToString().StartsWith(url))
            {
                return Response.Ok(response);
            }
            else
            {
                response.EnsureSuccessStatusCode();
            }
        }
        catch (HttpRequestException e)
        {
            return Response.Fail(e);
        }

        return Response.Ok();
    }).WithInit(async context =>
    {
        var response = await httpClient.PostAsJsonAsync(id, new { url });
        response.EnsureSuccessStatusCode();
    })
    .WithClean(async context =>
    {
        await httpClient.DeleteAsync(id);
    })
    .WithLoadSimulations(
        Simulation.Inject(rate: rate,
                          interval: TimeSpan.FromSeconds(intervalInSeconds),
                          during: TimeSpan.FromSeconds(durationInSeconds))
    );

    list[i] = getRedirects;
}



NBomberRunner.RegisterScenarios(list).Run();