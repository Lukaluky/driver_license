namespace OrderService.Domain.Entities;

public class ApplicationSummaryView
{
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime? LatestApplication { get; set; }
}
