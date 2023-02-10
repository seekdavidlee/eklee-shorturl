// See https://aka.ms/new-console-template for more information
using NBomber.CSharp;
using System.Net.Http.Json;

using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("XURL"));
httpClient.DefaultRequestHeaders.Add("API_KEY", Environment.GetEnvironmentVariable("XAPIKEY"));

var id = "A1234";
var scenario = Scenario.Create("get_redirects", async context =>
{
    try
    {
        var response = await httpClient.GetAsync(id);
        response.EnsureSuccessStatusCode();
    }
    catch (HttpRequestException e)
    {
        return Response.Fail(e);
    }

    return Response.Ok();
}).WithInit(async context =>
{
    var response = await httpClient.PostAsJsonAsync(id, new { url = $"https://{Guid.NewGuid():N}.com" });
    response.EnsureSuccessStatusCode();
})
.WithLoadSimulations(
    Simulation.Inject(rate: 10,
                      interval: TimeSpan.FromSeconds(1),
                      during: TimeSpan.FromSeconds(30))
);

NBomberRunner.RegisterScenarios(scenario).Run();