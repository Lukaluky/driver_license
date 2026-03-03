using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Data;
using StackExchange.Redis;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
        return Ok(new
        {
            Service = "OrderService.API",
            Version = version,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            UtcNow = DateTime.UtcNow
        });
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(
        [FromServices] AppDbContext dbContext,
        [FromServices] IConnectionMultiplexer redis)
    {
        var dbOk = await dbContext.Database.CanConnectAsync();
        var redisOk = redis.IsConnected;

        return Ok(new
        {
            Status = dbOk && redisOk ? "Healthy" : "Degraded",
            Database = dbOk ? "Up" : "Down",
            Redis = redisOk ? "Up" : "Down",
            UtcNow = DateTime.UtcNow
        });
    }
}
