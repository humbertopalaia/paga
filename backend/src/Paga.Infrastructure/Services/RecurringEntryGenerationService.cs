using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paga.Application.Abstractions;

namespace Paga.Infrastructure.Services;

/// <summary>
/// Hosted service that triggers recurring entry generation on a daily schedule.
/// Reads the schedule from configuration ("RecurringGeneration:ScheduleUtc"), defaults to 02:00 UTC.
/// Supports catch-up execution if the scheduled time has already passed for the current day.
/// </summary>
public class RecurringEntryGenerationService : BackgroundService
{
    private static readonly TimeOnly DefaultSchedule = new(2, 0);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringEntryGenerationService> _logger;
    private readonly TimeOnly _scheduledTime;

    private DateOnly _lastRunDate;

    public RecurringEntryGenerationService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RecurringEntryGenerationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _scheduledTime = ParseSchedule(configuration);
        _lastRunDate = DateOnly.MinValue;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RecurringEntryGenerationService started. Scheduled daily at {ScheduleUtc} UTC",
            _scheduledTime);

        // Catch-up: if scheduled time already passed today and no run recorded for today, execute immediately
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        if (currentTime > _scheduledTime && _lastRunDate < today)
        {
            _logger.LogInformation(
                "Scheduled time {ScheduleUtc} UTC already passed today. Executing catch-up run", _scheduledTime);
            await ExecuteGenerationAsync(today, stoppingToken);
        }

        // Main loop: wait until next scheduled time, then execute daily
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRun();

            _logger.LogDebug("Next generation run in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var runDate = DateOnly.FromDateTime(DateTime.UtcNow);

            if (_lastRunDate >= runDate)
            {
                // Already ran today (e.g., catch-up + timer aligned). Skip.
                continue;
            }

            await ExecuteGenerationAsync(runDate, stoppingToken);
        }

        _logger.LogInformation("RecurringEntryGenerationService stopping");
    }

    /// <summary>
    /// Executes one generation cycle: creates a scope, resolves the generator, and runs it.
    /// </summary>
    private async Task ExecuteGenerationAsync(DateOnly referenceDate, CancellationToken ct)
    {
        _logger.LogInformation("Recurring entry generation starting for {ReferenceDate}", referenceDate);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var generator = scope.ServiceProvider.GetRequiredService<IRecurringEntryGenerator>();

            await generator.GenerateAsync(referenceDate, ct);

            _lastRunDate = referenceDate;
            _logger.LogInformation("Recurring entry generation completed for {ReferenceDate}", referenceDate);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Let the host shut down gracefully
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recurring entry generation failed for {ReferenceDate}", referenceDate);
            // Record the run so we don't retry immediately in a tight loop
            _lastRunDate = referenceDate;
        }
    }

    /// <summary>
    /// Calculates the delay until the next scheduled execution time.
    /// </summary>
    private TimeSpan CalculateDelayUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var todayScheduled = now.Date.Add(_scheduledTime.ToTimeSpan());

        var nextRun = now < todayScheduled
            ? todayScheduled
            : todayScheduled.AddDays(1);

        return nextRun - now;
    }

    /// <summary>
    /// Parses the schedule from configuration. Falls back to 02:00 UTC on failure.
    /// </summary>
    private TimeOnly ParseSchedule(IConfiguration configuration)
    {
        var value = configuration["RecurringGeneration:ScheduleUtc"];

        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultSchedule;
        }

        if (TimeOnly.TryParse(value, out var parsed))
        {
            return parsed;
        }

        _logger.LogWarning(
            "Invalid RecurringGeneration:ScheduleUtc value '{ConfigValue}'. Using default 02:00 UTC",
            value);

        return DefaultSchedule;
    }
}
