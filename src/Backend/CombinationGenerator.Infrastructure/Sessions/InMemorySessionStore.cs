using System.Collections.Concurrent;
using CombinationGenerator.Application.Sessions;
using CombinationGenerator.Domain.Sessions;
using Microsoft.Extensions.Options;

namespace CombinationGenerator.Infrastructure.Sessions;

/// <summary>
/// המגירה בזיכרון של השרת. בטוחה לשימוש מהרבה בקשות בו-זמנית.
/// כל המידע נמחק כשהשרת נכבה; למעבר לאחסון חיצוני (Redis) כותבים מימוש אחר של ISessionStore.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();

    // מנעול ליצירה בלבד: הבדיקה "יש מקום?" וההוספה חייבות להיות צעד אחד
    private readonly object _createLock = new();

    private readonly SessionStoreOptions _options;
    private readonly TimeProvider _time;

    public InMemorySessionStore(IOptions<SessionStoreOptions> options, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(time);

        _options = options.Value;
        _time = time;

        if (!_options.IsValid)
        {
            throw new ArgumentException("SessionStore options must all be positive.", nameof(options));
        }
    }

    /// <summary>כמה Sessions יש כרגע במגירה, כולל כאלה שפגו ועוד לא נוקו.</summary>
    public int Count => _entries.Count;

    public Task<CreateResult> CreateAsync(GeneratorSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset now = _time.GetUtcNow();

        lock (_createLock)
        {
            if (_entries.Count >= _options.MaxSessions)
            {
                // לפני שמסרבים: אולי חלק מה-Sessions כבר פגו והמנקה עוד לא עבר
                RemoveExpired(now);

                if (_entries.Count >= _options.MaxSessions)
                {
                    return Task.FromResult(new CreateResult(CreateStatus.CapacityReached, null));
                }
            }

            StoredSession stored;
            do
            {
                // Guid חוזר על עצמו כמעט אף פעם; הלולאה רק מבטיחה שלא נדרוס במקרה הנדיר
                stored = new StoredSession(Guid.NewGuid(), Version: 1, session);
            }
            while (!_entries.TryAdd(stored.Id, new Entry(stored, now)));

            return Task.FromResult(new CreateResult(CreateStatus.Created, stored));
        }
    }

    public Task<StoredSession?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetLive(id, out Entry? entry))
        {
            return Task.FromResult<StoredSession?>(null);
        }

        entry.Touch(_time.GetUtcNow()); // נגעו בו: השעון מתאפס
        return Task.FromResult<StoredSession?>(entry.Value);
    }

    public Task<ReplaceResult> ReplaceAsync(Guid id, long expectedVersion, GeneratorSession next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetLive(id, out Entry? current))
        {
            return Task.FromResult(new ReplaceResult(ReplaceStatus.NotFound, null));
        }

        if (current.Value.Version != expectedVersion)
        {
            return Task.FromResult(new ReplaceResult(ReplaceStatus.VersionConflict, null));
        }

        var replaced = new StoredSession(id, expectedVersion + 1, next);
        var replacement = new Entry(replaced, _time.GetUtcNow());

        // הבדיקה וההחלפה כצעד אחד: מחליף רק אם הערך במילון הוא עדיין בדיוק current.
        // Entry הוא class בלי השוואה משלו, ולכן "בדיוק" פירושו אותו אובייקט בזיכרון.
        if (_entries.TryUpdate(id, replacement, current))
        {
            return Task.FromResult(new ReplaceResult(ReplaceStatus.Replaced, replaced));
        }

        // מישהו הספיק להחליף או למחוק בין הקריאה שלנו להחלפה
        ReplaceStatus status = _entries.ContainsKey(id) ? ReplaceStatus.VersionConflict : ReplaceStatus.NotFound;
        return Task.FromResult(new ReplaceResult(status, null));
    }

    public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_entries.TryRemove(id, out _));
    }

    /// <summary>
    /// מוחק את כל ה-Sessions שפגו ומחזיר כמה נמחקו. נקרא מהמנקה ברקע, ולפני סירוב בגלל תקרה.
    /// </summary>
    public int RemoveExpired() => RemoveExpired(_time.GetUtcNow());

    private int RemoveExpired(DateTimeOffset now)
    {
        int removed = 0;

        foreach (KeyValuePair<Guid, Entry> pair in _entries)
        {
            // מוחקים רק אם הערך עדיין אותו Entry שבדקנו: אם הוחלף בינתיים, הוא חדש ולא פג
            if (IsExpired(pair.Value, now) && _entries.TryRemove(pair))
            {
                removed++;
            }
        }

        return removed;
    }

    /// <summary>מוצא Session שלא פג. Session שפג נמחק בהזדמנות ומדווח כלא קיים.</summary>
    private bool TryGetLive(Guid id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Entry? entry)
    {
        if (!_entries.TryGetValue(id, out entry))
        {
            return false;
        }

        if (IsExpired(entry, _time.GetUtcNow()))
        {
            _entries.TryRemove(new KeyValuePair<Guid, Entry>(id, entry));
            entry = null;
            return false;
        }

        return true;
    }

    private bool IsExpired(Entry entry, DateTimeOffset now) =>
        now - entry.LastAccess >= _options.IdleTimeout;

    /// <summary>
    /// רשומה במגירה: ה-Session השמור, ומתי נגעו בו לאחרונה.
    /// הזמן נשמר בנפרד מ-StoredSession כדי שקריאה (שמאפסת את השעון) לא תיחשב "שינוי" ולא תפריע ל-TryUpdate.
    /// </summary>
    private sealed class Entry
    {
        private long _lastAccessTicks;

        public Entry(StoredSession value, DateTimeOffset lastAccess)
        {
            Value = value;
            _lastAccessTicks = lastAccess.UtcTicks;
        }

        public StoredSession Value { get; }

        // Interlocked: קריאה וכתיבה של long בטוחות גם כששתי בקשות נוגעות בו יחד
        public DateTimeOffset LastAccess => new(Interlocked.Read(ref _lastAccessTicks), TimeSpan.Zero);

        public void Touch(DateTimeOffset now) => Interlocked.Exchange(ref _lastAccessTicks, now.UtcTicks);
    }
}
