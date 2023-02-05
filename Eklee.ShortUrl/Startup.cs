using Eklee.ShortUrl;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Eklee.ShortUrl;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
        IConfiguration configuration = configBuilder.Build();
        builder.Services.AddSingleton(configuration);
    }
}
