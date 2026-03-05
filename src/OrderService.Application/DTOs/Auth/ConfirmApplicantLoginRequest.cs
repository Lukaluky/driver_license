namespace OrderService.Application.DTOs.Auth;

public record ConfirmApplicantLoginRequest(string Email, string Code);
