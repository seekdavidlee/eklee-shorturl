using Azure;
using Azure.Data.Tables;
using System;

namespace Eklee.ShortUrl;

public class UrlEntity : ITableEntity
{
    public string Url { get; set; }
    public string AllowedIPList { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
