using OrderService.Domain.Entities;
using OrderService.Domain.Enums;

namespace OrderService.Domain.Interfaces;

public interface IApplicationRepository
{
    Task<DriverApplication?> GetByIdAsync(Guid id);
    Task<DriverApplication?> GetByIdWithDetailsAsync(Guid id);
    Task<(List<DriverApplication> Items, int TotalCount)> GetByApplicantIdAsync(Guid applicantId, int page, int pageSize);
    Task<(List<DriverApplication> Items, int TotalCount)> GetAssignedToInspectorAsync(Guid inspectorId, int page, int pageSize);
    Task<(List<DriverApplication> Items, int TotalCount)> GetPendingForReviewAsync(int page, int pageSize);
    Task<List<ApplicationSummaryView>> GetSummaryAsync();
    Task<bool> HasIssuedCategoryAsync(Guid applicantId, LicenceCategory category);
    Task<string?> GetApplicantIinAsync(Guid applicantId);
    Task<bool> HasActiveApplicationAsync(Guid applicantId, LicenceCategory category);
    Task AddAsync(DriverApplication application);
    void Update(DriverApplication application);
}
