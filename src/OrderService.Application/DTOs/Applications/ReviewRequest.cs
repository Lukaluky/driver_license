namespace OrderService.Application.DTOs.Applications;

public record ReviewRequest(Guid ApplicationId, bool Approved, string? RejectionReason);
