namespace OrderService.Application.DTOs.Auth;

public record ConfirmEmailRequest(string Email, string Code);
