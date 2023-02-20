using Azure;
using Azure.Data.Tables;
using System;

namespace Eklee.ShortUrl;

public class VisitEntity : ITableEntity
{
    public string IP { get; set; }
    public DateTime Time { get; set; }
    public string Url { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
