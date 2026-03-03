using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Services;

public class ExternalCheckService : IExternalCheckService
{
    private readonly IReadOnlyCollection<IExternalCheckProvider> _providers;
    private readonly ILogger<ExternalCheckService> _logger;

    public ExternalCheckService(
        IEnumerable<IExternalCheckProvider> providers,
        ILogger<ExternalCheckService> logger)
    {
        _providers = providers.ToList();
        _logger = logger;
    }

    public async Task<ExternalChecksResult> RunAllAsync(string iin, CancellationToken cancellationToken = default)
    {
        var results = new List<ExternalCheckItemResult>();

        foreach (var provider in _providers)
        {
            try
            {
                var result = await provider.CheckAsync(iin, cancellationToken);
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
}
