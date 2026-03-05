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
    private readonly IValidator<RequestApplicantLoginCodeRequest> _requestApplicantLoginCodeValidator;
    private readonly IValidator<ConfirmApplicantLoginRequest> _confirmApplicantLoginValidator;

    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IEmailService emailService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RequestApplicantLoginCodeRequest> requestApplicantLoginCodeValidator,
        IValidator<ConfirmApplicantLoginRequest> confirmApplicantLoginValidator)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _emailService = emailService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _requestApplicantLoginCodeValidator = requestApplicantLoginCodeValidator;
        _confirmApplicantLoginValidator = confirmApplicantLoginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var normalizedEmail = NormalizeEmail(request.Email);
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);
        if (existingUser != null)
            throw new InvalidOperationException("Пользователь с таким email уже существует");

        var existingIinUser = await _unitOfWork.Users.GetByIinAsync(request.Iin);
        if (existingIinUser != null)
            throw new InvalidOperationException("Пользователь с таким ИИН уже существует");

        var confirmationCode = Random.Shared.Next(100000, 999999).ToString();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            Iin = request.Iin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Enum.Parse<UserRole>(request.Role),
            EmailConfirmed = false,
            EmailConfirmationCode = confirmationCode,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendEmailConfirmationAsync(user.Email, confirmationCode);

        return new AuthResponse(string.Empty, user.Email, user.Role.ToString());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Неверный email или пароль");

        if (user.Role != UserRole.Inspector)
            throw new InvalidOperationException("Вход по паролю доступен только для инспектора");

        if (!user.EmailConfirmed)
            throw new InvalidOperationException("Email не подтверждён. Проверьте почту");

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());
        return new AuthResponse(token, user.Email, user.Role.ToString());
    }

    public async Task RequestApplicantLoginCodeAsync(RequestApplicantLoginCodeRequest request)
    {
        var validation = await _requestApplicantLoginCodeValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);
        if (user == null)
            throw new KeyNotFoundException("Пользователь не найден");

        if (user.Role != UserRole.Applicant)
            throw new InvalidOperationException("Этот способ входа доступен только для заявителей");

        var loginCode = Random.Shared.Next(100000, 999999).ToString();
        user.EmailConfirmationCode = loginCode;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendEmailConfirmationAsync(user.Email, loginCode);
    }

    public async Task<AuthResponse> ConfirmApplicantLoginAsync(ConfirmApplicantLoginRequest request)
    {
        var validation = await _confirmApplicantLoginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);
        if (user == null)
            throw new KeyNotFoundException("Пользователь не найден");

        if (user.Role != UserRole.Applicant)
            throw new InvalidOperationException("Этот способ входа доступен только для заявителей");

        if (!string.Equals(user.EmailConfirmationCode, request.Code, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Неверный код входа");

        if (!user.EmailConfirmed)
            user.EmailConfirmed = true;

        user.EmailConfirmationCode = null;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());
        return new AuthResponse(token, user.Email, user.Role.ToString());
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

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

    public async Task ResendConfirmationCodeAsync(ResendConfirmationRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
            throw new KeyNotFoundException("Пользователь не найден");

        if (user.EmailConfirmed)
            throw new InvalidOperationException("Email уже подтвержден");

        var confirmationCode = Random.Shared.Next(100000, 999999).ToString();
        user.EmailConfirmationCode = confirmationCode;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendEmailConfirmationAsync(user.Email, confirmationCode);
    }
}
