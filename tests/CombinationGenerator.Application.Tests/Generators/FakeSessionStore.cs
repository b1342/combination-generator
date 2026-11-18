using CombinationGenerator.Application.Sessions;
using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Application.Tests.Generators;

/// <summary>
/// מגירה פשוטה לבדיקות של ה-Service, בלי זמן ובלי מקביליות.
/// מאפשרת "לזייף" מצבים שקשה לייצר: מגירה מלאה, ומישהו שמשנה את ה-Session באמצע בקשה.
/// </summary>
internal sealed class FakeSessionStore : ISessionStore
{
    private readonly Dictionary<Guid, StoredSession> _items = new();

    /// <summary>אם true, כל יצירה נדחית כאילו הגענו לתקרה.</summary>
    public bool IsFull { get; set; }

    /// <summary>רץ ממש לפני כל החלפה — כדי לדמות בקשה אחרת שהקדימה אותנו.</summary>
    public Action<Guid>? BeforeReplace { get; set; }

    public int Count => _items.Count;

    public Task<CreateResult> CreateAsync(GeneratorSession session, CancellationToken cancellationToken = default)
    {
        if (IsFull)
        {
            return Task.FromResult(new CreateResult(CreateStatus.CapacityReached, null));
        }

        var stored = new StoredSession(Guid.NewGuid(), 1, session);
        _items[stored.Id] = stored;
        return Task.FromResult(new CreateResult(CreateStatus.Created, stored));
    }

    public Task<StoredSession?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.TryGetValue(id, out var stored) ? stored : null);

    public Task<ReplaceResult> ReplaceAsync(Guid id, long expectedVersion, GeneratorSession next, CancellationToken cancellationToken = default)
    {
        BeforeReplace?.Invoke(id);

        if (!_items.TryGetValue(id, out var current))
        {
            return Task.FromResult(new ReplaceResult(ReplaceStatus.NotFound, null));
        }

        if (current.Version != expectedVersion)
        {
            return Task.FromResult(new ReplaceResult(ReplaceStatus.VersionConflict, null));
        }

        var replaced = new StoredSession(id, expectedVersion + 1, next);
        _items[id] = replaced;
        return Task.FromResult(new ReplaceResult(ReplaceStatus.Replaced, replaced));
    }

    public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Remove(id));

    /// <summary>מקדם את הגרסה בלי לשנות את ה-Session — כמו בקשה אחרת ששמרה רגע לפנינו.</summary>
    public void BumpVersion(Guid id) => _items[id] = _items[id] with { Version = _items[id].Version + 1 };
}
