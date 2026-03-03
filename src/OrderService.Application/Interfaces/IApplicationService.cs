using OrderService.Application.DTOs.Applications;
using OrderService.Application.DTOs.Common;

namespace OrderService.Application.Interfaces;

public interface IApplicationService
{
    Task<ApplicationResponse> CreateAsync(Guid applicantId, CreateApplicationRequest request);
    Task<ApplicationResponse> CreateRenewalAsync(Guid applicantId, RenewExpiredLicenceRequest request);
    Task<ApplicationResponse> CreateReissueAsync(Guid applicantId, CreateReissueApplicationRequest request);
    Task<ApplicationResponse> GetByIdForUserAsync(Guid applicationId, Guid userId, string role);
    Task<PagedResult<ApplicationResponse>> GetMyApplicationsAsync(Guid applicantId, int page, int pageSize);
    Task<PagedResult<ApplicationResponse>> GetAssignedToInspectorAsync(Guid inspectorId, int page, int pageSize);
    Task<PagedResult<ApplicationResponse>> GetPendingApplicationsAsync(int page, int pageSize);
    Task<List<ApplicationStatsResponse>> GetStatsAsync();
    Task<ApplicationResponse> CancelAsync(Guid applicationId, Guid applicantId);
    Task<ApplicationResponse> AssignToInspectorAsync(Guid applicationId, Guid inspectorId);
    Task<ApplicationResponse> ReviewAsync(Guid inspectorId, ReviewRequest request);
    Task<ApplicationResponse> PrintLicenceAsync(Guid applicationId, Guid inspectorId);
}
