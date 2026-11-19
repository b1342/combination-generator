import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';

/** שגיאה מתורגמת למסך: הקוד מהשרת (אם יש) והודעה למשתמש. */
export interface UiError {
  readonly code: string | null;
  readonly message: string;

  /**
   * true כשאין לנו דרך לדעת אם השרת ביצע את הפעולה: אין תשובה, עבר הזמן, או שגיאה פנימית (5xx).
   * במקרה כזה ה-Facade טוען את המצב האמיתי מהשרת, במקום לנחש.
   */
  readonly outcomeUnknown: boolean;
}

/**
 * מתרגם כל שגיאה להודעה בעברית.
 * השרת כבר שולח הודעה בעברית בשדה detail (ApiErrors.cs), אז משתמשים בה.
 * הקוד (למשל SESSION_NOT_FOUND) משמש את ה-Facade כדי להחליט מה לעשות.
 */
export function toUiError(error: unknown): UiError {
  if (error instanceof TimeoutError) {
    return { code: null, message: 'השרת לא הגיב בזמן. נסו שוב.', outcomeUnknown: true };
  }

  if (error instanceof HttpErrorResponse) {
    // status 0: אין תשובה מהשרת (השרת כבוי, אין רשת, או חסימת CORS)
    if (error.status === 0) {
      return { code: null, message: 'לא ניתן להתחבר לשרת. ודאו שהשרת רץ.', outcomeUnknown: true };
    }

    const outcomeUnknown = error.status >= 500;
    const body: unknown = error.error;
    if (isProblem(body)) {
      return {
        code: typeof body.code === 'string' ? body.code : null,
        message: typeof body.detail === 'string' ? body.detail : 'הבקשה אינה תקינה.',
        outcomeUnknown,
      };
    }

    return { code: null, message: 'אירעה שגיאה לא צפויה. נסו שוב.', outcomeUnknown };
  }

  return { code: null, message: 'אירעה שגיאה לא צפויה. נסו שוב.', outcomeUnknown: true };
}

/** האם הגוף נראה כמו ProblemDetails (אובייקט עם status). */
function isProblem(value: unknown): value is { code?: unknown; detail?: unknown } {
  return typeof value === 'object' && value !== null && 'status' in value;
}
