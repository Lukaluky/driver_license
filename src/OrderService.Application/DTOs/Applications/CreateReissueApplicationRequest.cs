namespace OrderService.Application.DTOs.Applications;

public record CreateReissueApplicationRequest(
    string Iin,
    string FullName,
    string Category,
    string Reason,
    DateTime? PreviousLicenceExpiredAt);
