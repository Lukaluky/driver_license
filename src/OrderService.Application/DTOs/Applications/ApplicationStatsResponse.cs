namespace OrderService.Application.DTOs.Applications;

public record ApplicationStatsResponse(
    string Category,
    int TotalCount,
    int PendingCount,
    int InProgressCount,
    int ApprovedCount,
    int RejectedCount,
    int PrintedCount,
    DateTime? OldestPendingCreatedAt,
    DateTime? LatestApplication);
