using PMS.Application.Application.Interface.Service;

namespace PMS.BackgroundJobs;

/// <summary>
/// Background service that runs daily and flips all overdue Active subscriptions to Expired.
/// Uses a scoped <see cref="ISubscriptionService"/> via <see cref="IServiceScopeFactory"/>
/// because <see cref="IHostedService"/> is a singleton but EF Core contexts are scoped.
/// </summary>
public class SubscriptionExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpiryJob> _logger;

    // How often to check for expired subscriptions
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    public SubscriptionExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionExpiryJob started. Interval: {Interval}", CheckInterval);

        // Run once on startup, then on the configured interval
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunExpiryCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during subscription expiry check.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunExpiryCheckAsync()
    {
        _logger.LogInformation("Running subscription expiry check at {Utc}", DateTime.UtcNow);

        // Create a new DI scope for each run so EF Core context is properly scoped
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        await service.ExpireOverdueSubscriptionsAsync();

        _logger.LogInformation("Subscription expiry check complete.");
    }
}
