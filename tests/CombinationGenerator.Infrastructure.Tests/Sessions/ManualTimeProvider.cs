namespace CombinationGenerator.Infrastructure.Tests.Sessions;

/// <summary>
/// שעון מזויף לבדיקות: הזמן זז רק כשהבדיקה אומרת לו. כך בודקים תפוגה של 30 דקות בלי לחכות 30 דקות.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
