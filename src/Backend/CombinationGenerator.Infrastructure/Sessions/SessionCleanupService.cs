using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CombinationGenerator.Infrastructure.Sessions;

/// <summary>
/// המנקה: רץ ברקע כל עוד השרת חי, ובכל מחזור מוחק Sessions שפגו.
/// בלעדיו, Session שאף אחד לא יבקש שוב היה נשאר בזיכרון לתמיד.
/// </summary>
public sealed class SessionCleanupService : BackgroundService
{
    private readonly InMemorySessionStore _store;
    private readonly SessionStoreOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(
        InMemorySessionStore store,
        IOptions<SessionStoreOptions> options,
        TimeProvider time,
        ILogger<SessionCleanupService> logger)
    {
        _store = store;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.CleanupInterval, _time);

        // מחכים למחזור הבא; כשהשרת נכבה stoppingToken מבוטל והלולאה נגמרת
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            int removed = _store.RemoveExpired();
            if (removed > 0)
            {
                _logger.LogInformation("Removed {Count} expired sessions.", removed);
            }
        }
    }
}
