namespace OrderService.Application.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string Role);
