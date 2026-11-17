namespace CombinationGenerator.Domain.Pagination;

/// <summary>
/// תיאור של עמוד אחד: איזה עמוד, מתוך כמה, מאיזה אינדקס הוא מתחיל וכמה שורות יש בו.
/// אין כאן את השורות עצמן; רק המספרים שמהם אפשר לבנות אותן.
/// </summary>
public readonly record struct PageWindow(long PageNumber, long TotalPages, long StartIndex, int Count)
{
    /// <summary>האינדקס של השורה האחרונה בעמוד. "חזור" חוזר אליו.</summary>
    public long LastIndex => StartIndex + Count - 1;
}
