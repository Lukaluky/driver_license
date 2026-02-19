using OrderService.Application.DTOs.Auth;

namespace OrderService.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task ConfirmEmailAsync(ConfirmEmailRequest request);
}
