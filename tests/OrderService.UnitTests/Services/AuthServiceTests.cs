using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OrderService.Application.DTOs.Auth;
using OrderService.Application.Interfaces;
using OrderService.Application.Services;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using Xunit;

namespace OrderService.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IValidator<RegisterRequest>> _registerValidator = new();
    private readonly Mock<IValidator<LoginRequest>> _loginValidator = new();
    private readonly Mock<IValidator<RequestApplicantLoginCodeRequest>> _requestApplicantLoginCodeValidator = new();
    private readonly Mock<IValidator<ConfirmApplicantLoginRequest>> _confirmApplicantLoginValidator = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authService = new AuthService(
            _unitOfWork.Object,
            _tokenService.Object,
            _emailService.Object,
            _registerValidator.Object,
            _loginValidator.Object,
            _requestApplicantLoginCodeValidator.Object,
            _confirmApplicantLoginValidator.Object);
    }

    [Fact]
    public async Task Register_Should_Create_User_And_Return_Empty_Token()
    {
        var request = new RegisterRequest("test@test.com", "Password1!", "Applicant", "900515300123");
        _registerValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _unitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _authService.RegisterAsync(request);

        result.Token.Should().BeEmpty();
        result.Email.Should().Be("test@test.com");
        result.Role.Should().Be("Applicant");
        _unitOfWork.Verify(u => u.Users.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Register_Should_Throw_When_Email_Exists()
    {
        var request = new RegisterRequest("existing@test.com", "Password1!", "Applicant", "900515300124");
        _registerValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _unitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(new User { Email = request.Email });

        var act = () => _authService.RegisterAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*уже существует*");
    }

    [Fact]
    public async Task Login_Should_Return_Token_For_Valid_Credentials()
    {
        var request = new LoginRequest("test@test.com", "Password1!");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            Role = UserRole.Inspector,
            EmailConfirmed = true
        };

        _loginValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _unitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _tokenService.Setup(t => t.GenerateToken(user.Id, user.Email, "Inspector"))
            .Returns("login-token");

        var result = await _authService.LoginAsync(request);

        result.Token.Should().Be("login-token");
    }

    [Fact]
    public async Task Login_Should_Throw_When_Email_Not_Confirmed()
    {
        var request = new LoginRequest("test@test.com", "Password1!");
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            Role = UserRole.Inspector,
            EmailConfirmed = false
        };

        _loginValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _unitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var act = () => _authService.LoginAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*не подтверждён*");
    }

    [Fact]
    public async Task Login_Should_Throw_For_Applicant_Password_Login()
    {
        var request = new LoginRequest("test@test.com", "Password1!");
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            Role = UserRole.Applicant,
            EmailConfirmed = true
        };

        _loginValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _unitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var act = () => _authService.LoginAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*только для инспектора*");
    }
}
