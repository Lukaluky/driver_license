using FluentValidation;
using Mapster;
using OrderService.Application.DTOs.Applications;
using OrderService.Application.DTOs.Common;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class ApplicationService : IApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly IValidator<CreateApplicationRequest> _createValidator;
    private readonly IValidator<ReviewRequest> _reviewValidator;

    public ApplicationService(
        IUnitOfWork unitOfWork,
        ICacheService cache,
        IValidator<CreateApplicationRequest> createValidator,
        IValidator<ReviewRequest> reviewValidator)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _createValidator = createValidator;
        _reviewValidator = reviewValidator;
    }

    public async Task<ApplicationResponse> CreateAsync(Guid applicantId, CreateApplicationRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var category = Enum.Parse<LicenceCategory>(request.Category, true);

        var lockKey = $"app-lock:{applicantId}:{category}";
        var acquired = await _cache.LockAsync(lockKey, TimeSpan.FromMinutes(5));
        if (!acquired)
            throw new InvalidOperationException(
                $"У вас уже есть активная заявка на категорию {category}. Дождитесь её рассмотрения");

        try
        {
            var hasActive = await _unitOfWork.Applications.HasActiveApplicationAsync(applicantId, category);
            if (hasActive)
                throw new InvalidOperationException(
                    $"У вас уже есть активная заявка на категорию {category}. Дождитесь её рассмотрения");

            var application = new DriverApplication
            {
                Id = Guid.NewGuid(),
                ApplicantId = applicantId,
                Iin = request.Iin,
                FullName = request.FullName,
                Category = category,
                Status = ApplicationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Applications.AddAsync(application);
            await _unitOfWork.SaveChangesAsync();

            return application.Adapt<ApplicationResponse>();
        }
        catch
        {
            await _cache.ReleaseLockAsync(lockKey);
            throw;
        }
    }

    public async Task<PagedResult<ApplicationResponse>> GetMyApplicationsAsync(Guid applicantId, int page, int pageSize)
    {
        var (items, totalCount) = await _unitOfWork.Applications.GetByApplicantIdAsync(applicantId, page, pageSize);

        return new PagedResult<ApplicationResponse>
        {
            Items = items.Adapt<List<ApplicationResponse>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<ApplicationResponse>> GetPendingApplicationsAsync(int page, int pageSize)
    {
        var cacheKey = $"pending-apps:{page}:{pageSize}";
        var cached = await _cache.GetAsync<PagedResult<ApplicationResponse>>(cacheKey);
        if (cached != null) return cached;

        var (items, totalCount) = await _unitOfWork.Applications.GetPendingForReviewAsync(page, pageSize);

        var result = new PagedResult<ApplicationResponse>
        {
            Items = items.Adapt<List<ApplicationResponse>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(30));
        return result;
    }

    public async Task<ApplicationResponse> AssignToInspectorAsync(Guid applicationId, Guid inspectorId)
    {
        var app = await _unitOfWork.Applications.GetByIdAsync(applicationId)
            ?? throw new KeyNotFoundException("Заявка не найдена");

        if (app.Status != ApplicationStatus.ExternalChecksPassed)
            throw new InvalidOperationException(
                $"Заявку нельзя назначить. Текущий статус: {app.Status}");

        app.InspectorId = inspectorId;
        app.Status = ApplicationStatus.AssignedToInspector;
        app.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        await InvalidatePendingCache();

        return app.Adapt<ApplicationResponse>();
    }

    public async Task<ApplicationResponse> ReviewAsync(Guid inspectorId, ReviewRequest request)
    {
        var validation = await _reviewValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var app = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId)
            ?? throw new KeyNotFoundException("Заявка не найдена");

        if (app.Status != ApplicationStatus.AssignedToInspector)
            throw new InvalidOperationException(
                $"Заявку нельзя рассмотреть. Текущий статус: {app.Status}");

        if (app.InspectorId != inspectorId)
            throw new InvalidOperationException("Заявка назначена другому инспектору");

        app.Status = request.Approved ? ApplicationStatus.Approved : ApplicationStatus.Rejected;
        app.RejectionReason = request.Approved ? null : request.RejectionReason;
        app.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        if (!request.Approved)
        {
            var lockKey = $"app-lock:{app.ApplicantId}:{app.Category}";
            await _cache.ReleaseLockAsync(lockKey);
        }

        return app.Adapt<ApplicationResponse>();
    }

    public async Task<ApplicationResponse> PrintLicenceAsync(Guid applicationId, Guid inspectorId)
    {
        var app = await _unitOfWork.Applications.GetByIdAsync(applicationId)
            ?? throw new KeyNotFoundException("Заявка не найдена");

        if (app.Status != ApplicationStatus.Approved)
            throw new InvalidOperationException(
                $"Печать невозможна. Текущий статус: {app.Status}");

        var card = new LicenceCard
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            CardNumber = $"DL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            Category = app.Category,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(10)
        };

        await _unitOfWork.AddLicenceCardAsync(card);
        app.Status = ApplicationStatus.Printed;
        app.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        var lockKey = $"app-lock:{app.ApplicantId}:{app.Category}";
        await _cache.ReleaseLockAsync(lockKey);

        return app.Adapt<ApplicationResponse>();
    }

    private async Task InvalidatePendingCache()
    {
        for (int i = 1; i <= 10; i++)
        {
            await _cache.RemoveAsync($"pending-apps:{i}:10");
            await _cache.RemoveAsync($"pending-apps:{i}:20");
        }
    }
}
