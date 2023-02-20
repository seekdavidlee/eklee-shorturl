using System;

namespace Eklee.ShortUrl;

public class VisitStat
{
    public int VisitCount { get; set; }
    public DateTime? FirstVist { get; set; }
    public DateTime? LastVist { get; set; }
}