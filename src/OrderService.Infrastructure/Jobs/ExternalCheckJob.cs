using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.Jobs;

public class ExternalCheckJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExternalCheckService _externalCheckService;
    private readonly IEmailService _emailService;
    private readonly ICacheService _cache;
    private readonly ILogger<ExternalCheckJob> _logger;

    public ExternalCheckJob(
        IUnitOfWork unitOfWork,
        IExternalCheckService externalCheckService,
        IEmailService emailService,
        ICacheService cache,
        ILogger<ExternalCheckJob> logger)
    {
        _unitOfWork = unitOfWork;
        _externalCheckService = externalCheckService;
        _emailService = emailService;
        _cache = cache;
        _logger = logger;
    }

    public async Task RunChecksAsync(Guid applicationId)
    {
        _logger.LogInformation("Starting external checks for application {AppId}", applicationId);

        var app = await _unitOfWork.Applications.GetByIdWithDetailsAsync(applicationId);
        if (app == null)
        {
            _logger.LogWarning("Application {AppId} not found", applicationId);
            return;
        }

        if (app.Status != ApplicationStatus.Pending)
        {
            _logger.LogWarning("Application {AppId} is not in Pending status", applicationId);
            return;
        }

        app.Status = ApplicationStatus.ExternalChecksInProgress;
        app.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var mvdPassed = await _externalCheckService.CheckMvdAsync(app.Iin);
        var medicalPassed = await _externalCheckService.CheckMedicalAsync(app.Iin);

        app.MvdCheckPassed = mvdPassed;
        app.MedicalCheckPassed = medicalPassed;

        if (mvdPassed && medicalPassed)
        {
            app.Status = ApplicationStatus.ExternalChecksPassed;
            _logger.LogInformation("External checks PASSED for application {AppId}", applicationId);
        }
        else
        {
            app.Status = ApplicationStatus.ExternalChecksFailed;
            app.RejectionReason = BuildFailureReason(mvdPassed, medicalPassed);
            _logger.LogInformation("External checks FAILED for application {AppId}: {Reason}",
                applicationId, app.RejectionReason);

            var lockKey = $"app-lock:{app.ApplicantId}:{app.Category}";
            await _cache.ReleaseLockAsync(lockKey);
        }

        app.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        if (app.Applicant != null)
        {
            await _emailService.SendApplicationStatusAsync(
                app.Applicant.Email, app.Id.ToString(), app.Status.ToString());
        }
    }

    private static string BuildFailureReason(bool mvd, bool medical)
    {
        var reasons = new List<string>();
        if (!mvd) reasons.Add("Проверка МВД не пройдена");
        if (!medical) reasons.Add("Медицинская проверка не пройдена");
        return string.Join("; ", reasons);
    }
}
