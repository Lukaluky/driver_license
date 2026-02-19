using OrderService.Domain.Enums;

namespace OrderService.Domain.Entities;

public class DriverApplication
{
    public Guid Id { get; set; }
    public Guid ApplicantId { get; set; }
    public string Iin { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public LicenceCategory Category { get; set; }
    public ApplicationStatus Status { get; set; }
    public Guid? InspectorId { get; set; }
    public string? RejectionReason { get; set; }
    public bool? MvdCheckPassed { get; set; }
    public bool? MedicalCheckPassed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Applicant { get; set; } = null!;
    public User? Inspector { get; set; }
    public LicenceCard? LicenceCard { get; set; }
}
