namespace OrderService.Application.Interfaces;

public interface IExternalCheckService
{
    Task<ExternalChecksResult> RunAllAsync(string iin, CancellationToken cancellationToken = default);
}

public interface IExternalCheckProvider
{
    string Name { get; }
    Task<ExternalCheckItemResult> CheckAsync(string iin, CancellationToken cancellationToken = default);
}

public sealed record ExternalCheckItemResult(string Name, bool Passed, string? Message = null);

public sealed record ExternalChecksResult(IReadOnlyList<ExternalCheckItemResult> Items)
{
    public bool AllPassed => Items.All(x => x.Passed);
}
