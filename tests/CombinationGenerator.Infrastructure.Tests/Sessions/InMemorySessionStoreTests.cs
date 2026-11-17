using CombinationGenerator.Application.Sessions;
using CombinationGenerator.Domain.Sessions;
using CombinationGenerator.Infrastructure.Sessions;
using Microsoft.Extensions.Options;

namespace CombinationGenerator.Infrastructure.Tests.Sessions;

public class InMemorySessionStoreTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(30);

    private readonly ManualTimeProvider _time = new();

    private InMemorySessionStore CreateStore(int maxSessions = 1000) =>
        new(Options.Create(new SessionStoreOptions { IdleTimeoutMinutes = 30, MaxSessions = maxSessions }), _time);

    private static GeneratorSession NewSession() => GeneratorSession.Create(3);

    private static async Task<StoredSession> CreateOrFail(InMemorySessionStore store)
    {
        var result = await store.CreateAsync(NewSession());
        Assert.Equal(CreateStatus.Created, result.Status);
        var stored = result.Stored;
        Assert.NotNull(stored);
        return stored;
    }

    // ---------- יצירה וקריאה ----------

    [Fact]
    public async Task Create_AssignsNewIdAndVersionOne_AndCanBeReadBack()
    {
        var store = CreateStore();
        var session = NewSession();

        var created = (await store.CreateAsync(session)).Stored;

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(1, created.Version);
        Assert.Same(session, (await store.GetAsync(created.Id))?.Session);
    }

    [Fact]
    public async Task Create_TwoSessions_GetDifferentIds()
    {
        var store = CreateStore();

        var first = await CreateOrFail(store);
        var second = await CreateOrFail(store);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNull()
    {
        Assert.Null(await CreateStore().GetAsync(Guid.NewGuid()));
    }

    // ---------- החלפה וגרסאות ----------

    [Fact]
    public async Task Replace_WithCurrentVersion_Succeeds_AndBumpsVersion()
    {
        var store = CreateStore();
        var created = await CreateOrFail(store);
        var next = NewSession();

        var result = await store.ReplaceAsync(created.Id, expectedVersion: 1, next);

        Assert.Equal(ReplaceStatus.Replaced, result.Status);
        Assert.Equal(2, result.Stored?.Version);
        Assert.Same(next, (await store.GetAsync(created.Id))?.Session);
    }

    [Fact]
    public async Task TwoTabs_ReadSameVersion_OnlyFirstReplaceWins()
    {
        // התרחיש מההדמיה: שתי לשוניות קוראות גרסה 1, שתיהן מנסות להחליף
        var store = CreateStore();
        var created = await CreateOrFail(store);
        var fromTab1 = NewSession();
        var fromTab2 = NewSession();

        var tab1 = await store.ReplaceAsync(created.Id, 1, fromTab1);
        var tab2 = await store.ReplaceAsync(created.Id, 1, fromTab2);

        Assert.Equal(ReplaceStatus.Replaced, tab1.Status);
        Assert.Equal(ReplaceStatus.VersionConflict, tab2.Status);
        Assert.Null(tab2.Stored);

        var stored = await store.GetAsync(created.Id);
        Assert.Same(fromTab1, stored?.Session); // לשונית 2 לא דרסה
        Assert.Equal(2, stored?.Version);
    }

    [Fact]
    public async Task Replace_UnknownId_IsNotFound()
    {
        var result = await CreateStore().ReplaceAsync(Guid.NewGuid(), 1, NewSession());

        Assert.Equal(ReplaceStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ManyParallelReplaces_OnSameVersion_ExactlyOneWins()
    {
        var store = CreateStore();
        var created = await CreateOrFail(store);

        var results = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => store.ReplaceAsync(created.Id, 1, NewSession()))));

        Assert.Single(results, r => r.Status == ReplaceStatus.Replaced);
        Assert.Equal(49, results.Count(r => r.Status == ReplaceStatus.VersionConflict));
        Assert.Equal(2, (await store.GetAsync(created.Id))?.Version);
    }

    // ---------- מחיקה ----------

    [Fact]
    public async Task Remove_DeletesSession_AndSecondRemoveReturnsFalse()
    {
        var store = CreateStore();
        var created = await CreateOrFail(store);

        Assert.True(await store.RemoveAsync(created.Id));
        Assert.Null(await store.GetAsync(created.Id));
        Assert.False(await store.RemoveAsync(created.Id));
    }

    // ---------- תפוגה ----------

    [Fact]
    public async Task Session_ExpiresAfterIdleTimeout()
    {
        var store = CreateStore();
        var created = await CreateOrFail(store);

        _time.Advance(Timeout);

        Assert.Null(await store.GetAsync(created.Id));
        Assert.Equal(ReplaceStatus.NotFound, (await store.ReplaceAsync(created.Id, 1, NewSession())).Status);
    }

    [Fact]
    public async Task Session_JustBeforeTimeout_IsStillAlive()
    {
        var store = CreateStore();
        var created = await CreateOrFail(store);

        _time.Advance(Timeout - TimeSpan.FromSeconds(1));

        Assert.NotNull(await store.GetAsync(created.Id));
    }

    [Fact]
    public async Task ActiveUser_NeverExpires_BecauseEveryAccessResetsTheClock()
    {
        var store = CreateStore();
        var created = await CreateOrFail(store);

        // שעתיים של עבודה, לחיצה כל 20 דקות
        for (int i = 0; i < 6; i++)
        {
            _time.Advance(TimeSpan.FromMinutes(20));
            Assert.NotNull(await store.GetAsync(created.Id));
        }
    }

    [Fact]
    public async Task Replace_AlsoResetsTheClock()
    {
        var store = CreateStore();
        var created = await CreateOrFail(store);

        _time.Advance(TimeSpan.FromMinutes(20));
        await store.ReplaceAsync(created.Id, 1, NewSession());
        _time.Advance(TimeSpan.FromMinutes(20)); // 40 דקות מהיצירה, 20 מההחלפה

        Assert.NotNull(await store.GetAsync(created.Id));
    }

    [Fact]
    public async Task RemoveExpired_RemovesOnlyExpiredSessions()
    {
        var store = CreateStore();
        var old = await CreateOrFail(store);
        _time.Advance(TimeSpan.FromMinutes(20));
        var recent = await CreateOrFail(store);
        _time.Advance(TimeSpan.FromMinutes(15)); // old: 35 דקות, recent: 15 דקות

        int removed = store.RemoveExpired();

        Assert.Equal(1, removed);
        Assert.Equal(1, store.Count);
        Assert.Null(await store.GetAsync(old.Id));
        Assert.NotNull(await store.GetAsync(recent.Id));
    }

    // ---------- תקרה ----------

    [Fact]
    public async Task Create_WhenFull_ReturnsCapacityReached()
    {
        var store = CreateStore(maxSessions: 2);
        await CreateOrFail(store);
        await CreateOrFail(store);

        var third = await store.CreateAsync(NewSession());

        Assert.Equal(CreateStatus.CapacityReached, third.Status);
        Assert.Null(third.Stored);
        Assert.Equal(2, store.Count);
    }

    [Fact]
    public async Task Create_WhenFull_FirstCleansExpired_ThenSucceeds()
    {
        var store = CreateStore(maxSessions: 2);
        await CreateOrFail(store);
        await CreateOrFail(store);

        _time.Advance(Timeout); // שתיהן פגו, המנקה עוד לא עבר

        var third = await store.CreateAsync(NewSession());

        Assert.Equal(CreateStatus.Created, third.Status);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task ManyParallelCreates_NeverExceedCapacity()
    {
        var store = CreateStore(maxSessions: 10);

        var results = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => store.CreateAsync(NewSession()))));

        Assert.Equal(10, results.Count(r => r.Status == CreateStatus.Created));
        Assert.Equal(40, results.Count(r => r.Status == CreateStatus.CapacityReached));
        Assert.Equal(10, store.Count);
    }

    // ---------- הגדרות ----------

    [Theory]
    [InlineData(0, 1000, 60)]
    [InlineData(30, 0, 60)]
    [InlineData(30, 1000, 0)]
    public void InvalidOptions_Throw(int timeout, int max, int cleanup)
    {
        var options = Options.Create(new SessionStoreOptions
        {
            IdleTimeoutMinutes = timeout,
            MaxSessions = max,
            CleanupIntervalSeconds = cleanup,
        });

        Assert.Throws<ArgumentException>(() => new InMemorySessionStore(options, _time));
    }
}
