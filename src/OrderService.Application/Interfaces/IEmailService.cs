namespace OrderService.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string email, string code);
    Task SendApplicationStatusAsync(string email, string applicationId, string status);
}
