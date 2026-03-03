namespace OrderService.Domain.Entities;

public class ApplicationSummaryView
{
    public string Category { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PrintedCount { get; set; }
    public DateTime? OldestPendingCreatedAt { get; set; }
    public DateTime? LatestApplication { get; set; }
}
