using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Services;

public sealed class MedicalExternalCheckProvider : IExternalCheckProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _wiremockBaseUrl;
    public string Name => "Medical";

    public MedicalExternalCheckProvider(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _wiremockBaseUrl = config["ExternalApi:WiremockUrl"] ?? "http://localhost:8080";
    }

    public async Task<ExternalCheckItemResult> CheckAsync(string iin, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_wiremockBaseUrl}/api/medical/check?iin={iin}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new ExternalCheckItemResult(Name, false, $"HTTP {(int)response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<ExternalCheckPayload>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return new ExternalCheckItemResult(Name, payload?.Passed ?? false, payload?.Message);
    }

    private sealed class ExternalCheckPayload
    {
        public bool Passed { get; set; }
        public string? Message { get; set; }
    }
}
