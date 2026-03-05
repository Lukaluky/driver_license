using OrderService.Application.DTOs.Auth;

namespace OrderService.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request); // Inspector login by password
    Task RequestApplicantLoginCodeAsync(RequestApplicantLoginCodeRequest request);
    Task<AuthResponse> ConfirmApplicantLoginAsync(ConfirmApplicantLoginRequest request);
    Task ConfirmEmailAsync(ConfirmEmailRequest request);
    Task ResendConfirmationCodeAsync(ResendConfirmationRequest request);
}
