using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Services;

public class ExternalCheckService : IExternalCheckService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalCheckService> _logger;

    public ExternalCheckService(
        AppDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<ExternalCheckService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ExternalChecksResult> RunAllAsync(string iin, CancellationToken cancellationToken = default)
    {
        var providers = await _dbContext.ExternalCheckProviders
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.ExecutionOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var results = new List<ExternalCheckItemResult>();
        if (providers.Count == 0)
            return new ExternalChecksResult(results);

        foreach (var provider in providers)
        {
            try
            {
                var result = await RunSingleProviderAsync(provider, iin, cancellationToken);
                results.Add(result);
                _logger.LogInformation(
                    "External check {Provider} for IIN {Iin}: {Result}",
                    provider.Name, iin, result.Passed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External check provider {Provider} crashed for IIN {Iin}", provider.Name, iin);
                results.Add(new ExternalCheckItemResult(provider.Name, false, "Provider error"));
            }
        }

        return new ExternalChecksResult(results);
    }

    private async Task<ExternalCheckItemResult> RunSingleProviderAsync(
        Domain.Entities.ExternalCheckProviderConfiguration provider,
        string iin,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("external-checks");
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, provider.TimeoutSeconds));

        var method = new HttpMethod(provider.HttpMethod);
        var url = BuildProviderUrl(provider, iin);

        using var request = new HttpRequestMessage(method, url);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new ExternalCheckItemResult(provider.Name, false, $"HTTP {(int)response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<ExternalCheckPayload>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return new ExternalCheckItemResult(provider.Name, payload?.Passed ?? false, payload?.Message);
    }

    private static string BuildProviderUrl(Domain.Entities.ExternalCheckProviderConfiguration provider, string iin)
    {
        var path = provider.Path.Replace("{iin}", iin, StringComparison.OrdinalIgnoreCase);
        if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            return path;

        if (!path.Contains("{iin}", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("iin=", StringComparison.OrdinalIgnoreCase))
        {
            path += path.Contains('?') ? $"&iin={iin}" : $"?iin={iin}";
        }

        return $"{provider.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private sealed class ExternalCheckPayload
    {
        public bool Passed { get; set; }
        public string? Message { get; set; }
    }
}
