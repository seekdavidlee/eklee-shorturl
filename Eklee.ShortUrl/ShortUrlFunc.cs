using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure;
using Microsoft.Extensions.Configuration;

namespace Eklee.ShortUrl
{
    public class ShortUrlFunc
    {
        private readonly IConfiguration configuration;

        public ShortUrlFunc(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [FunctionName(nameof(GetShortUrl))]
        public async Task<IActionResult> GetShortUrl(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{id}")] HttpRequest req,
            [Table("Url", Connection = "UrlStorageConnection")] TableClient tableClient,
            string id)
        {
            AsyncPageable<UrlEntity> queryResults = tableClient.QueryAsync<UrlEntity>(filter: $"PartitionKey eq 'common' and RowKey eq '{id}'");
            await foreach (UrlEntity entity in queryResults)
            {
                return new RedirectResult(entity.Url);
            }

            return new NotFoundResult();
        }

        [FunctionName(nameof(AddOrUpdateShortUrl))]
        public async Task<IActionResult> AddOrUpdateShortUrl(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "{id}")] HttpRequest req,
            [Table("Url", Connection = "UrlStorageConnection")] TableClient tableClient,
            string id)
        {
            if (!req.Headers.TryGetValue("API_KEY", out var apiKey) || apiKey != this.configuration["API_KEY"])
            {
                return new UnauthorizedResult();
            }

            var body = await req.ReadAsStringAsync();
            await tableClient.UpsertEntityAsync(new UrlEntity { RowKey = id, PartitionKey = "common", Url = body });
            return new OkResult();
        }

        [FunctionName(nameof(DeleteShortUrl))]
        public async Task<IActionResult> DeleteShortUrl(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "{id}")] HttpRequest req,
            [Table("Url", Connection = "UrlStorageConnection")] TableClient tableClient,
            string id)
        {
            if (!req.Headers.TryGetValue("API_KEY", out var apiKey) || apiKey != this.configuration["API_KEY"])
            {
                return new UnauthorizedResult();
            }

            await tableClient.DeleteEntityAsync("common", id);

            return new NoContentResult();

        }
    }
}
