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

        // TryGetLive גם "נוגע" ב-Session: השעון שלו מתאפס
        if (!TryGetLive(id, out Entry? entry))
        {
            return Task.FromResult<StoredSession?>(null);
        }

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
            // שני תנאים, כדי שלא נמחק Session שמישהו נגע בו ממש עכשיו:
            // TryClaimExpired מסמן אותו "מת" רק אם לא נגעו בו מאז שנמצא פג (CAS על זמן הגישה),
            // ו-TryRemove(pair) מוחק רק אם הערך הוא עדיין אותו Entry (לא הוחלף בינתיים)
            if (pair.Value.TryClaimExpired(now, _options.IdleTimeout) && _entries.TryRemove(pair))
            {
                removed++;
            }
        }

        return removed;
    }

    /// <summary>
    /// מוצא Session חי ו"נוגע" בו (מאפס את השעון), בצעד אטומי אחד.
    /// Session שפג, או שהמנקה כבר סימן למחיקה, מדווח כלא קיים ונמחק בהזדמנות.
    /// </summary>
    private bool TryGetLive(Guid id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Entry? entry)
    {
        if (!_entries.TryGetValue(id, out entry))
        {
            return false;
        }

        DateTimeOffset now = _time.GetUtcNow();
        if (!entry.TryTouch(now, _options.IdleTimeout))
        {
            if (entry.TryClaimExpired(now, _options.IdleTimeout))
            {
                _entries.TryRemove(new KeyValuePair<Guid, Entry>(id, entry));
            }

            entry = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// רשומה במגירה: ה-Session השמור, ומתי נגעו בו לאחרונה.
    /// הזמן נשמר בנפרד מ-StoredSession כדי שקריאה (שמאפסת את השעון) לא תיחשב "שינוי" ולא תפריע ל-TryUpdate.
    /// </summary>
    /// <remarks>
    /// הנגיעה והמחיקה מתחרות על אותו שדה, _lastAccessTicks, ושתיהן משנות אותו רק ב-CompareExchange.
    /// לכן תמיד רק אחת מנצחת: או שהבקשה נגעה והמנקה מוותר, או שהמנקה סימן "מת" והבקשה מקבלת "לא קיים".
    /// </remarks>
    private sealed class Entry
    {
        /// <summary>ערך מיוחד: המנקה תפס את הרשומה למחיקה. אי אפשר יותר לנגוע בה.</summary>
        private const long Claimed = long.MinValue;

        private long _lastAccessTicks;

        public Entry(StoredSession value, DateTimeOffset lastAccess)
        {
            Value = value;
            _lastAccessTicks = lastAccess.UtcTicks;
        }

        public StoredSession Value { get; }

        /// <summary>
        /// מאפס את השעון ומחזיר true, אם הרשומה עדיין חיה. אם פגה או נתפסה למחיקה, מחזיר false.
        /// הזמן רק מתקדם: נגיעה עם זמן ישן לא דורסת נגיעה חדשה יותר.
        /// </summary>
        public bool TryTouch(DateTimeOffset now, TimeSpan idleTimeout)
        {
            long nowTicks = now.UtcTicks;
            while (true)
            {
                long seen = Interlocked.Read(ref _lastAccessTicks);
                if (seen == Claimed || nowTicks - seen >= idleTimeout.Ticks)
                {
                    return false;
                }

                if (seen >= nowTicks || Interlocked.CompareExchange(ref _lastAccessTicks, nowTicks, seen) == seen)
                {
                    return true;
                }

                // מישהו אחר נגע בדיוק עכשיו: מנסים שוב עם הערך החדש
            }
        }

        /// <summary>
        /// מסמן את הרשומה "מתה" ומחזיר true, רק אם היא פגה ואף אחד לא נגע בה מאז שנבדקה.
        /// אם כבר סומנה קודם, גם מחזיר true (אפשר למחוק).
        /// </summary>
        public bool TryClaimExpired(DateTimeOffset now, TimeSpan idleTimeout)
        {
            long seen = Interlocked.Read(ref _lastAccessTicks);
            if (seen == Claimed)
            {
                return true;
            }

            if (now.UtcTicks - seen < idleTimeout.Ticks)
            {
                return false;
            }

            return Interlocked.CompareExchange(ref _lastAccessTicks, Claimed, seen) == seen;
        }
    }
}
