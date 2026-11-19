import { HttpErrorResponse } from '@angular/common/http';

/** שגיאה מתורגמת למסך: הקוד מהשרת (אם יש) והודעה למשתמש. */
export interface UiError {
  readonly code: string | null;
  readonly message: string;
}

/**
 * מתרגם כל שגיאה להודעה בעברית.
 * השרת כבר שולח הודעה בעברית בשדה detail (ApiErrors.cs), אז משתמשים בה.
 * הקוד (למשל SESSION_NOT_FOUND) משמש את ה-Facade כדי להחליט מה לעשות.
 */
export function toUiError(error: unknown): UiError {
  if (error instanceof HttpErrorResponse) {
    // status 0: הבקשה לא הגיעה לשרת בכלל (השרת כבוי, אין רשת, או חסימת CORS)
    if (error.status === 0) {
      return { code: null, message: 'לא ניתן להתחבר לשרת. ודאו שהשרת רץ.' };
    }

    const body: unknown = error.error;
    if (isProblem(body)) {
      return {
        code: typeof body.code === 'string' ? body.code : null,
        message: typeof body.detail === 'string' ? body.detail : 'הבקשה אינה תקינה.',
      };
    }
  }

  return { code: null, message: 'אירעה שגיאה לא צפויה. נסו שוב.' };
}

/** האם הגוף נראה כמו ProblemDetails (אובייקט עם status). */
function isProblem(value: unknown): value is { code?: unknown; detail?: unknown } {
  return typeof value === 'object' && value !== null && 'status' in value;
}
