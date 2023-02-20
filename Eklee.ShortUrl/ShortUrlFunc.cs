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
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;
using System;
using System.Linq;

namespace Eklee.ShortUrl;

public class ShortUrlFunc
{
    private readonly IConfiguration configuration;
    private readonly HashSet<string> bannedList;
    private readonly bool isSmokeTesting;
    public ShortUrlFunc(IConfiguration configuration)
    {
        this.configuration = configuration;
        var list = this.configuration["BannedList"]?.Split(',');
        this.bannedList = new HashSet<string>(list ?? Array.Empty<string>())
        {
            "swagger",
            "stats"
        };

        var smokeTest = configuration["SMOKE_TEST"];
        if (string.IsNullOrWhiteSpace(smokeTest))
        {
            throw new ArgumentNullException(nameof(smokeTest));
        }
        isSmokeTesting = bool.Parse(smokeTest);
    }

    [OpenApiIgnore]
    [FunctionName(nameof(SwaggerUiRoute))]
    public IActionResult SwaggerUiRoute(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger")] HttpRequest req)
    {
        return new RedirectResult("/swagger/ui");
    }

    [OpenApiOperation(operationId: nameof(GetShortUrl), Summary = "Gets the url.", Description = "This API call will return your url based on a key provided.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "The key.", Description = "The key to the URL.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Redirect, contentType: "text/plain", bodyType: typeof(string), Summary = "Redirection URL.", Description = "The response will redirect the browser to the intended URL.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "If the key is not found.", Description = "The response will return a Not Found response if the key is not found.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Summary = "If the key is found but there is an IP restriction.", Description = "The response will return a Forbidden if your IP does not match the expected IP address.")]
    [FunctionName(nameof(GetStats))]
    public async Task<IActionResult> GetStats(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stats/{year:int}")] HttpRequest req,
    [Table("Visit", Connection = "UrlStorageConnection")] TableClient statTableClient,
    int year,
    CancellationToken cancellationToken)
    {
        if (year < DateTime.UtcNow.AddYears(-5).Year) 
        {
            return new BadRequestResult();
        }

        AsyncPageable<VisitEntity> queryResults = statTableClient.QueryAsync<VisitEntity>(filter: $"PartitionKey eq '{year}'", cancellationToken: cancellationToken);
        var h = new Dictionary<string, VisitStat>();
        await foreach (VisitEntity entity in queryResults)
        {
            var parts = entity.RowKey.Split('.');
            string k = parts[0];
            if (h.ContainsKey(k))
            {
                var vStat = h[k];
                vStat.VisitCount += 1;
                if (entity.Time > vStat.LastVist)
                {
                    vStat.LastVist = entity.Time;
                }

                if (entity.Time < vStat.FirstVist)
                {
                    vStat.FirstVist = entity.Time;
                }
            }
            else
            {
                h.Add(k, new VisitStat
                {
                    VisitCount = 1,
                    FirstVist = entity.Time,
                    LastVist = entity.Time,
                    Key = k
                });
            }
        }

        return new OkObjectResult(new Stats
        {
            Year = year,
            VisitStats = h.Values.ToList(),
        });
    }

    [OpenApiOperation(operationId: nameof(GetShortUrl), Summary = "Gets the url.", Description = "This API call will return your url based on a key provided.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "The key.", Description = "The key to the URL.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Redirect, contentType: "text/plain", bodyType: typeof(string), Summary = "Redirection URL.", Description = "The response will redirect the browser to the intended URL.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "If the key is not found.", Description = "The response will return a Not Found response if the key is not found.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Summary = "If the key is found but there is an IP restriction.", Description = "The response will return a Forbidden if your IP does not match the expected IP address.")]
    [FunctionName(nameof(GetShortUrl))]
    public async Task<IActionResult> GetShortUrl(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{id:regex(^((?!swagger|stats).)*$)}")] HttpRequest req,
    [Table("Url", Connection = "UrlStorageConnection")] TableClient urlTableClient,
    [Table("Visit", Connection = "UrlStorageConnection")] TableClient statTableClient,
    string id, CancellationToken cancellationToken)
    {
        AsyncPageable<UrlEntity> queryResults = urlTableClient.QueryAsync<UrlEntity>(filter: $"PartitionKey eq 'common' and RowKey eq '{id}'", cancellationToken: cancellationToken);
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

            await statTableClient.AddEntityAsync(new VisitEntity
            {
                RowKey = $"{id}.{Guid.NewGuid():n}",
                PartitionKey = $"{DateTime.UtcNow.Year}",
                IP = req.HttpContext.Connection.RemoteIpAddress.ToString(),
                Time = DateTime.UtcNow,
                Url = entity.Url,
            }, cancellationToken);

            return new RedirectResult(entity.Url);
        }

        return new NotFoundResult();
    }

    [OpenApiOperation(operationId: nameof(AddOrUpdateShortUrl), Summary = "Create or update the url.", Description = "This API call create or update based on a key and url provided.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "The key.", Description = "The key to the URL.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("api_key", SecuritySchemeType.ApiKey, Name = "API_KEY", In = OpenApiSecurityLocationType.Header)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UrlRequest), Description = "The request body contains the URL to redirect to.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Error message", Description = "The response will be the error message.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Summary = "OK", Description = "The response will return OK response if the entity is updated.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Summary = "Unauthorized", Description = "The response will return Unauthorized response if the key does not match or there is a IP restriction.")]
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

        if (this.bannedList.Contains(id))
        {
            return new BadRequestObjectResult(new { errorMessage = $"Id {id} is not allowed." });
        }

        try
        {
            var dto = await JsonSerializer.DeserializeAsync<UrlRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            new Uri(dto.Url);

            await tableClient.UpsertEntityAsync(new UrlEntity
            {
                RowKey = id,
                PartitionKey = "common",
                Url = dto.Url,
                AllowedIPList = dto.AllowedIPList
            });

            return new OkResult();
        }
        catch (UriFormatException)
        {
            return new BadRequestObjectResult(new { errorMessage = "Url is invalid." });
        }
        catch (JsonException jsonEx)
        {
            return new BadRequestObjectResult(new { errorMessage = jsonEx.Message });
        }
    }


    [OpenApiOperation(operationId: nameof(DeleteShortUrl), Summary = "Deletes the url.", Description = "This API call delete the URL entry.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "The key.", Description = "The key to the URL.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("api_key", SecuritySchemeType.ApiKey, Name = "API_KEY", In = OpenApiSecurityLocationType.Header)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "OK", Description = "The response will return No Content response if the entity is deleted.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Summary = "Unauthorized", Description = "The response will return Unauthorized response if the key does not match or there is a IP restriction.")]
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

        if (!isSmokeTesting && !IpList.Contains(req.HttpContext.Connection.RemoteIpAddress.ToString()))
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
