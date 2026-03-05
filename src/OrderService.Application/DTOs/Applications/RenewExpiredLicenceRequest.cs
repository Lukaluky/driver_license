namespace OrderService.Application.DTOs.Applications;

public record RenewExpiredLicenceRequest(
    string FullName,
    string Category,
    DateTime ExpiredAt);
