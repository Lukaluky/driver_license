using OrderService.Application.DTOs.Applications;
using OrderService.Application.DTOs.Common;

namespace OrderService.Application.Interfaces;

public interface IApplicationService
{
    Task<ApplicationResponse> CreateAsync(Guid applicantId, CreateApplicationRequest request);
    Task<PagedResult<ApplicationResponse>> GetMyApplicationsAsync(Guid applicantId, int page, int pageSize);
    Task<PagedResult<ApplicationResponse>> GetPendingApplicationsAsync(int page, int pageSize);
    Task<ApplicationResponse> AssignToInspectorAsync(Guid applicationId, Guid inspectorId);
    Task<ApplicationResponse> ReviewAsync(Guid inspectorId, ReviewRequest request);
    Task<ApplicationResponse> PrintLicenceAsync(Guid applicationId, Guid inspectorId);
}
