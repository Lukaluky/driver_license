using OrderService.Domain.Enums;

namespace OrderService.Domain.Entities;

public class LicenceCard
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public LicenceCategory Category { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public DriverApplication Application { get; set; } = null!;
}
