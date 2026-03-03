using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.Services;

public sealed class RabbitMqEmailConsumer : BackgroundService
{
    private const string QueueName = "email-notifications";

    private readonly ILogger<RabbitMqEmailConsumer> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqEmailConsumer(IConfiguration config, ILogger<RabbitMqEmailConsumer> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TryConnect())
        {
            _logger.LogWarning("RabbitMQ consumer is disabled because connection failed.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delivery = _channel!.BasicGet(QueueName, autoAck: false);
            if (delivery is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            try
            {
                var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                var message = JsonSerializer.Deserialize<EmailNotificationMessage>(json);
                if (message is null || string.IsNullOrWhiteSpace(message.To))
                {
                    _logger.LogWarning("RabbitMQ email message is invalid: {Payload}", json);
                    _channel.BasicAck(delivery.DeliveryTag, multiple: false);
                    continue;
                }

                await SendEmailAsync(message, stoppingToken);
                _channel.BasicAck(delivery.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process email message from RabbitMQ");
                _channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }

    private bool TryConnect()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
                UserName = _config["RabbitMQ:UserName"] ?? "guest",
                Password = _config["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ consumer connection failed");
            return false;
        }
    }

    private async Task SendEmailAsync(EmailNotificationMessage message, CancellationToken cancellationToken)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning(
                "SMTP host is not configured. Email to {Email} not sent. Subject: {Subject}",
                message.To, message.Subject);
            return;
        }

        var from = _config["Smtp:From"] ?? "noreply@driver-licence.local";
        var port = int.TryParse(_config["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var enableSsl = bool.TryParse(_config["Smtp:EnableSsl"], out var parsedEnableSsl) && parsedEnableSsl;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var user = _config["Smtp:User"];
        var password = _config["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, password);

        using var mail = new MailMessage(from, message.To, message.Subject, message.Body);
        await client.SendMailAsync(mail, cancellationToken);
        _logger.LogInformation("Email delivered to {Email}. Type: {Type}", message.To, message.Type);
    }

    private sealed record EmailNotificationMessage(string Type, string To, string Subject, string Body);
}
