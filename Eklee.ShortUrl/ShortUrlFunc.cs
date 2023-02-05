using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Azure.Data.Tables;
using Azure;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text.Json;

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
                if (entity.AllowedIPList != null)
                {
                    var hashSet = new HashSet<string>(entity.AllowedIPList.Split(','));
                    if (!hashSet.Contains(req.HttpContext.Connection.RemoteIpAddress.ToString()))
                    {
                        return new UnauthorizedResult();
                    }
                }

                var queryStrings = req.GetQueryParameterDictionary();
                if (queryStrings.TryGetValue("action", out var action))
                {
                    if (action == "lookup")
                    {
                        return new JsonResult(new { entity.Url });
                    }
                }

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
            if (!ValidateRequest(req))
            {
                return new UnauthorizedResult();
            }

            try
            {
                var dto = JsonSerializer.Deserialize<UrlRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                await tableClient.UpsertEntityAsync(new UrlEntity
                {
                    RowKey = id,
                    PartitionKey = "common",
                    Url = dto.Url,
                    AllowedIPList = dto.AllowedIPList
                });

                return new OkResult();
            }
            catch (JsonException jsonEx)
            {
                return new BadRequestObjectResult(new { errorMessage = jsonEx.Message });
            }
        }

        [FunctionName(nameof(DeleteShortUrl))]
        public async Task<IActionResult> DeleteShortUrl(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "{id}")] HttpRequest req,
            [Table("Url", Connection = "UrlStorageConnection")] TableClient tableClient,
            string id)
        {
            if (!ValidateRequest(req))
            {
                return new UnauthorizedResult();
            }

            await tableClient.DeleteEntityAsync("common", id);

            return new NoContentResult();

        }

        private static readonly HashSet<string> IpList = new HashSet<string>();

        private bool ValidateRequest(HttpRequest req)
        {
            if (IpList.Count == 0)
            {
                foreach (var ip in this.configuration["ALLOWED_IP_LIST"].Split(','))
                {
                    IpList.Add(ip.Trim());
                }
            }

            if (!IpList.Contains(req.HttpContext.Connection.RemoteIpAddress.ToString()))
            {
                return false;
            }

            if (!req.Headers.TryGetValue("API_KEY", out var apiKey) || apiKey != this.configuration["API_KEY"])
            {
                return false;
            }

            return true;
        }
    }
}
