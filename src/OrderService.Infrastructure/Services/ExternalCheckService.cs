using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Services;

public class ExternalCheckService : IExternalCheckService
{
    private readonly HttpClient _httpClient;
    private readonly string _wiremockBaseUrl;
    private readonly ILogger<ExternalCheckService> _logger;

    public ExternalCheckService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<ExternalCheckService> logger)
    {
        _httpClient = httpClient;
        _wiremockBaseUrl = config["ExternalApi:WiremockUrl"] ?? "http://localhost:8080";
        _logger = logger;
    }

    public async Task<bool> CheckMvdAsync(string iin)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_wiremockBaseUrl}/api/mvd/check?iin={iin}");
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ExternalCheckResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("MVD check for IIN {Iin}: {Result}", iin, result?.Passed);
            return result?.Passed ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MVD check failed for IIN {Iin}", iin);
            return false;
        }
    }

    public async Task<bool> CheckMedicalAsync(string iin)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_wiremockBaseUrl}/api/medical/check?iin={iin}");
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ExternalCheckResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("Medical check for IIN {Iin}: {Result}", iin, result?.Passed);
            return result?.Passed ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Medical check failed for IIN {Iin}", iin);
            return false;
        }
    }

    private class ExternalCheckResult
    {
        public bool Passed { get; set; }
        public string? Message { get; set; }
    }
}
