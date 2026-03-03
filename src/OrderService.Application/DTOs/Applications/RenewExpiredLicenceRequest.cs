namespace OrderService.Application.DTOs.Applications;

public record RenewExpiredLicenceRequest(
    string Iin,
    string FullName,
    string Category,
    DateTime ExpiredAt);
