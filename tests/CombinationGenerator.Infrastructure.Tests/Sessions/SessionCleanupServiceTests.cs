using CombinationGenerator.Domain.Sessions;
using CombinationGenerator.Infrastructure.Sessions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace CombinationGenerator.Infrastructure.Tests.Sessions;

/// <summary>
/// בודק את המנקה עצמו (ולא רק את RemoveExpired): שהוא באמת רץ ברקע לפי הטיימר ומוחק Sessions שפגו.
/// FakeTimeProvider מזיז גם את השעון וגם את הטיימרים, כך ש-31 דקות עוברות במילישניות.
/// </summary>
public class SessionCleanupServiceTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly IOptions<SessionStoreOptions> _options =
        Options.Create(new SessionStoreOptions { IdleTimeoutMinutes = 30, CleanupIntervalSeconds = 60, MaxSessions = 1000 });

    [Fact]
    public async Task ExpiredSession_IsRemovedByTheBackgroundLoop()
    {
        var store = new InMemorySessionStore(_options, _time);
        await store.CreateAsync(GeneratorSession.Create(3));
        var service = new SessionCleanupService(store, _options, _time, NullLogger<SessionCleanupService>.Instance);

        await service.StartAsync(CancellationToken.None);
        try
        {
            _time.Advance(TimeSpan.FromMinutes(31));

            // הלולאה רצה ברקע: נותנים לה כמה מחזורים להגיע ל-RemoveExpired
            for (int attempt = 0; attempt < 50 && store.Count > 0; attempt++)
            {
                _time.Advance(Interval);
                await Task.Delay(20);
            }

            Assert.Equal(0, store.Count);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ActiveSession_IsNotRemoved()
    {
        var store = new InMemorySessionStore(_options, _time);
        await store.CreateAsync(GeneratorSession.Create(3));
        var service = new SessionCleanupService(store, _options, _time, NullLogger<SessionCleanupService>.Instance);

        await service.StartAsync(CancellationToken.None);
        try
        {
            // 10 מחזורי ניקוי = 10 דקות, הרבה פחות מ-30
            for (int tick = 0; tick < 10; tick++)
            {
                _time.Advance(Interval);
                await Task.Delay(20);
            }

            Assert.Equal(1, store.Count);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Stop_EndsTheLoopCleanly()
    {
        var store = new InMemorySessionStore(_options, _time);
        var service = new SessionCleanupService(store, _options, _time, NullLogger<SessionCleanupService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // אחרי עצירה המשימה של הלולאה הסתיימה (ולא נשארה תלויה או קרסה)
        Assert.NotNull(service.ExecuteTask);
        Assert.True(service.ExecuteTask!.IsCompleted);
    }
}
