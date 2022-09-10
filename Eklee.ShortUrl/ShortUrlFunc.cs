using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Microsoft.Azure.WebJobs.Host.Bindings.OpenType;
using Microsoft.WindowsAzure.Storage.Table;
using Azure.Data.Tables;
using Azure;

namespace Eklee.ShortUrl
{
	public static class ShortUrlFunc
	{
		[FunctionName("ShortUrl")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{id}")] HttpRequest req,
			[Table("Url", Connection = "UrlStorageConnection")] TableClient tableClient,
			string id,
			ILogger log)
		{
			AsyncPageable<UrlEntity> queryResults = tableClient.QueryAsync<UrlEntity>(filter: $"PartitionKey eq 'common' and RowKey eq '{id}'");
			await foreach (UrlEntity entity in queryResults)
			{
				return new RedirectResult(entity.Url);
			}

			return new NotFoundResult();
		}
	}
}
