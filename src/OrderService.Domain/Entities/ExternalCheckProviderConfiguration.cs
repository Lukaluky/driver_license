namespace OrderService.Domain.Entities;

public class ExternalCheckProviderConfiguration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public int TimeoutSeconds { get; set; } = 10;
    public bool IsEnabled { get; set; } = true;
    public int ExecutionOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
