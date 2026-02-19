namespace OrderService.Application.DTOs.Applications;

public record ApplicationResponse(
    Guid Id,
    string Iin,
    string FullName,
    string Category,
    string Status,
    bool? MvdCheckPassed,
    bool? MedicalCheckPassed,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
