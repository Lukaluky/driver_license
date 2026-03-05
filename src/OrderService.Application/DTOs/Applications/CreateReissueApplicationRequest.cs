namespace OrderService.Application.DTOs.Applications;

public record CreateReissueApplicationRequest(
    string FullName,
    string Category,
    string Reason,
    DateTime? PreviousLicenceExpiredAt);
