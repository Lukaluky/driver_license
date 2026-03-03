using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10
        });
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-Id";
    if (!context.Request.Headers.TryGetValue(headerName, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
        correlationId = Guid.NewGuid().ToString("N");

    context.Response.Headers[headerName] = correlationId.ToString();
    await next();
});

app.UseRateLimiter();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Gateway" }));
app.MapGet("/", () => Results.Ok(new
{
    Service = "DriverLicenceSystem Gateway",
    Routes = new[] { "/api/orders/*", "/api/*", "/swagger/*", "/hangfire/*" }
}));
app.MapReverseProxy();

app.Run();
