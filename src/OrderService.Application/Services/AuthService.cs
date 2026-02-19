using FluentValidation;
using OrderService.Application.DTOs.Auth;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IEmailService emailService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _emailService = emailService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Пользователь с таким email уже существует");

        var confirmationCode = Random.Shared.Next(100000, 999999).ToString();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Enum.Parse<UserRole>(request.Role),
            EmailConfirmed = false,
            EmailConfirmationCode = confirmationCode,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendEmailConfirmationAsync(user.Email, confirmationCode);

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());
        return new AuthResponse(token, user.Email, user.Role.ToString());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Неверный email или пароль");

        if (!user.EmailConfirmed)
            throw new InvalidOperationException("Email не подтверждён. Проверьте почту");

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());
        return new AuthResponse(token, user.Email, user.Role.ToString());
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
            throw new KeyNotFoundException("Пользователь не найден");

        if (user.EmailConfirmed)
            throw new InvalidOperationException("Email уже подтверждён");

        if (user.EmailConfirmationCode != request.Code)
            throw new InvalidOperationException("Неверный код подтверждения");

        user.EmailConfirmed = true;
        user.EmailConfirmationCode = null;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }
}
