using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.Services;

public class RabbitMqEmailService : IEmailService, IDisposable
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMqEmailService> _logger;
    private const string QueueName = "email-notifications";

    public RabbitMqEmailService(IConfiguration config, ILogger<RabbitMqEmailService> logger)
    {
        _logger = logger;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
                UserName = config["RabbitMQ:UserName"] ?? "guest",
                Password = config["RabbitMQ:Password"] ?? "guest"
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ connection failed. Email notifications will be logged only");
        }
    }

    public Task SendEmailConfirmationAsync(string email, string code)
    {
        var message = new
        {
            Type = "EmailConfirmation",
            To = email,
            Subject = "Подтверждение email",
            Body = $"Ваш код подтверждения: {code}"
        };
        PublishMessage(message);
        _logger.LogInformation("Email confirmation sent to {Email}, code: {Code}", email, code);
        return Task.CompletedTask;
    }

    public Task SendApplicationStatusAsync(string email, string applicationId, string status)
    {
        var message = new
        {
            Type = "ApplicationStatus",
            To = email,
            Subject = "Статус заявки обновлён",
            Body = $"Заявка {applicationId}: статус изменён на {status}"
        };
        PublishMessage(message);
        _logger.LogInformation("Status notification sent to {Email} for application {AppId}", email, applicationId);
        return Task.CompletedTask;
    }

    private void PublishMessage(object message)
    {
        if (_channel == null) return;

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;

        _channel.BasicPublish(exchange: "", routingKey: QueueName, basicProperties: props, body: body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
