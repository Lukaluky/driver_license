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
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authService = new AuthService(
            _unitOfWork.Object,
            _tokenService.Object,
            _emailService.Object,
            _registerValidator.Object,
            _loginValidator.Object);
    }

    [Fact]
    public async Task Register_Should_Create_User_And_Return_Token()
    {
        var request = new RegisterRequest("test@test.com", "Password1!", "Applicant");
        _registerValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _unitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<Guid>(), request.Email, "Applicant"))
            .Returns("test-token");
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _authService.RegisterAsync(request);

        result.Token.Should().Be("test-token");
        result.Email.Should().Be("test@test.com");
        result.Role.Should().Be("Applicant");
        _unitOfWork.Verify(u => u.Users.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Register_Should_Throw_When_Email_Exists()
    {
        var request = new RegisterRequest("existing@test.com", "Password1!", "Applicant");
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
            Role = UserRole.Applicant,
            EmailConfirmed = true
        };

        _loginValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _unitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _tokenService.Setup(t => t.GenerateToken(user.Id, user.Email, "Applicant"))
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
}
