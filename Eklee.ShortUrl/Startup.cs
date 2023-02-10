using Eklee.ShortUrl;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Eklee.ShortUrl;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
        IConfiguration configuration = configBuilder.Build();
        builder.Services.AddSingleton(configuration);
        builder.Services.Configure<HttpOptions>(config =>
        {
            config.RoutePrefix = string.Empty;
        }).AddSingleton<IOpenApiConfigurationOptions>(_ =>
        {
            var options = new OpenApiConfigurationOptions()
            {
                Info = new OpenApiInfo()
                {
                    Version = "1.0",
                    Title = "Short Url Service",
                    Description = "Host your very own URL redirection service using Azure Functions.",
                    License = new OpenApiLicense()
                    {
                        Name = "MIT",
                        Url = new Uri("http://opensource.org/licenses/MIT"),
                    }
                },
            };

            return options;
        });
    }
}
