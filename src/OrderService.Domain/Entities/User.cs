using OrderService.Domain.Enums;

namespace OrderService.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationCode { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<DriverApplication> Applications { get; set; } = new List<DriverApplication>();
    public ICollection<DriverApplication> InspectedApplications { get; set; } = new List<DriverApplication>();
}
