import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';
import { describe, expect, it } from 'vitest';
import { toUiError } from './generator-errors';

describe('toUiError', () => {
  it('uses the code and the Hebrew detail from ProblemDetails', () => {
    const error = new HttpErrorResponse({
      status: 404,
      error: { status: 404, code: 'SESSION_NOT_FOUND', detail: 'ה-Session לא קיים או שפג תוקפו. יש להתחיל מחדש.' },
    });

    expect(toUiError(error)).toEqual({
      code: 'SESSION_NOT_FOUND',
      message: 'ה-Session לא קיים או שפג תוקפו. יש להתחיל מחדש.',
      outcomeUnknown: false,
    });
  });

  it('marks status 0 (no response) as unknown outcome', () => {
    const result = toUiError(new HttpErrorResponse({ status: 0 }));

    expect(result.code).toBeNull();
    expect(result.outcomeUnknown).toBe(true);
    expect(result.message).toContain('לא ניתן להתחבר לשרת');
  });

  it('marks a timeout as unknown outcome', () => {
    const result = toUiError(new TimeoutError());

    expect(result.outcomeUnknown).toBe(true);
    expect(result.message).toContain('לא הגיב בזמן');
  });

  it('marks 5xx as unknown outcome, 4xx as known', () => {
    expect(toUiError(new HttpErrorResponse({ status: 500, error: { status: 500 } })).outcomeUnknown).toBe(true);
    expect(toUiError(new HttpErrorResponse({ status: 409, error: { status: 409, code: 'NO_NEXT_PAGE' } })).outcomeUnknown).toBe(false);
  });

  it('falls back to a generic message when the body is not ProblemDetails', () => {
    const result = toUiError(new HttpErrorResponse({ status: 400, error: 'plain text' }));

    expect(result.code).toBeNull();
    expect(result.message).toBe('אירעה שגיאה לא צפויה. נסו שוב.');
  });

  it('treats any other thrown value as unknown outcome', () => {
    expect(toUiError(new Error('boom')).outcomeUnknown).toBe(true);
  });
});
