using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LocalChat.Endpoints.HealthCheck;

public class DefaultHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}