using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OrderService.Application.DTOs.Applications;
using OrderService.Application.Interfaces;
using OrderService.Application.Services;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using Xunit;

namespace OrderService.UnitTests.Services;

public class ApplicationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<IValidator<CreateApplicationRequest>> _createValidator = new();
    private readonly Mock<IValidator<ReviewRequest>> _reviewValidator = new();
    private readonly ApplicationService _service;

    public ApplicationServiceTests()
    {
        _service = new ApplicationService(
            _unitOfWork.Object,
            _cache.Object,
            _createValidator.Object,
            _reviewValidator.Object);
    }

    [Fact]
    public async Task Create_Should_Return_Application_When_Valid()
    {
        var userId = Guid.NewGuid();
        var request = new CreateApplicationRequest("900515300123", "Иванов Иван", "B");

        _createValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _cache.Setup(c => c.LockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        _unitOfWork.Setup(u => u.Applications.HasActiveApplicationAsync(userId, LicenceCategory.B))
            .ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.CreateAsync(userId, request);

        result.Iin.Should().Be("900515300123");
        result.Category.Should().Be("B");
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Create_Should_Throw_When_Active_Application_Exists()
    {
        var userId = Guid.NewGuid();
        var request = new CreateApplicationRequest("900515300123", "Иванов Иван", "B");

        _createValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _cache.Setup(c => c.LockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        _unitOfWork.Setup(u => u.Applications.HasActiveApplicationAsync(userId, LicenceCategory.B))
            .ReturnsAsync(true);

        var act = () => _service.CreateAsync(userId, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*активная заявка*");
    }

    [Fact]
    public async Task Create_Should_Throw_When_Lock_Not_Acquired()
    {
        var userId = Guid.NewGuid();
        var request = new CreateApplicationRequest("900515300123", "Иванов Иван", "B");

        _createValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _cache.Setup(c => c.LockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);

        var act = () => _service.CreateAsync(userId, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*активная заявка*");
    }

    [Fact]
    public async Task Review_Should_Approve_Application()
    {
        var inspectorId = Guid.NewGuid();
        var app = new DriverApplication
        {
            Id = Guid.NewGuid(),
            ApplicantId = Guid.NewGuid(),
            Iin = "900515300123",
            FullName = "Тест",
            Category = LicenceCategory.B,
            Status = ApplicationStatus.AssignedToInspector,
            InspectorId = inspectorId,
            CreatedAt = DateTime.UtcNow
        };
        var request = new ReviewRequest(app.Id, true, null);

        _reviewValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _unitOfWork.Setup(u => u.Applications.GetByIdAsync(app.Id))
            .ReturnsAsync(app);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.ReviewAsync(inspectorId, request);

        result.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task Review_Should_Reject_And_Release_Lock()
    {
        var inspectorId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var app = new DriverApplication
        {
            Id = Guid.NewGuid(),
            ApplicantId = applicantId,
            Iin = "900515300123",
            FullName = "Тест",
            Category = LicenceCategory.B,
            Status = ApplicationStatus.AssignedToInspector,
            InspectorId = inspectorId,
            CreatedAt = DateTime.UtcNow
        };
        var request = new ReviewRequest(app.Id, false, "Не соответствует требованиям");

        _reviewValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _unitOfWork.Setup(u => u.Applications.GetByIdAsync(app.Id))
            .ReturnsAsync(app);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.ReviewAsync(inspectorId, request);

        result.Status.Should().Be("Rejected");
        _cache.Verify(c => c.ReleaseLockAsync($"app-lock:{applicantId}:B"), Times.Once);
    }
}
