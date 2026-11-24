/** גדלי העמוד שהמשתמשת יכולה לבחור. השרת מגביל ל-200 (Generator:MaxPageSize). */
export const PAGE_SIZE_OPTIONS: readonly number[] = [5, 10, 20, 50, 100];

/**
 * גדלי העמוד שמציעים, לפי כמה קומבינציות יש: כל גודל שקטן מהסה"כ, ועוד גודל אחד שכבר מציג את כולן בדף אחד.
 * למשל 6 קומבינציות: 5, 10. ‏20 קומבינציות: 5, 10, 20. אין טעם להציע 100 כשיש 6.
 * הסה"כ מגיע כטקסט (עד 20!), ולכן ההשוואה ב-BigInt.
 */
export function pageSizeOptionsFor(totalCount: string | null): readonly number[] {
  if (totalCount === null || !/^\d+$/.test(totalCount)) {
    return PAGE_SIZE_OPTIONS;
  }

  const total = BigInt(totalCount);
  const coversAll = PAGE_SIZE_OPTIONS.findIndex(size => BigInt(size) >= total);
  return coversAll === -1 ? PAGE_SIZE_OPTIONS : PAGE_SIZE_OPTIONS.slice(0, coversAll + 1);
}

/** גודל העמוד בכניסה הראשונה ל"כל הקומבינציות". */
export const DEFAULT_PAGE_SIZE = 20;

/** כמה זמן מחכים לתשובה מהשרת לפני שמוותרים. בלי זה, בקשה תקועה הייתה נועלת את כל המסך. */
export const REQUEST_TIMEOUT_MS = 30_000;
