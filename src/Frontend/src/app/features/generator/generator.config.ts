/** גדלי העמוד שהמשתמשת יכולה לבחור. השרת מגביל ל-200 (Generator:MaxPageSize). */
export const PAGE_SIZE_OPTIONS: readonly number[] = [5, 10, 20, 50, 100];

/** גודל העמוד בכניסה הראשונה ל"כל הקומבינציות". */
export const DEFAULT_PAGE_SIZE = 20;

/** כמה זמן מחכים לתשובה מהשרת לפני שמוותרים. בלי זה, בקשה תקועה הייתה נועלת את כל המסך. */
export const REQUEST_TIMEOUT_MS = 30_000;
