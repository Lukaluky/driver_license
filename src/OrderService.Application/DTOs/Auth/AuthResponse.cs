namespace OrderService.Application.DTOs.Auth;

public record AuthResponse(string Token, string Email, string Role);
