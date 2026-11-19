import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { GeneratorApiService } from './data-access/generator-api.service';
import { PageAction, PageNavigation, SessionDto } from './data-access/generator-api.models';
import { toUiError } from './data-access/generator-errors';
import { SessionIdStorage } from './data-access/session-id-storage';
import { DEFAULT_PAGE_SIZE } from './generator.config';

/**
 * "המנהל" של המסך. המקבילה של GeneratorService בשרת.
 * שומר מה מוצג עכשיו (Signals), ולכל כפתור: שולח בקשה, ומעדכן את המצב לפי התשובה.
 * הרכיבים רק קוראים ממנו וקוראים לפונקציות שלו; הם לא מדברים עם השרת.
 * לא מחשב שום דבר: כל המספרים מגיעים מהשרת.
 */
@Injectable()
export class GeneratorFacade {
  private readonly api = inject(GeneratorApiService);
  private readonly storage = inject(SessionIdStorage);
  private readonly destroyRef = inject(DestroyRef);

  // ---------- המצב (פרטי: רק ה-Facade משנה אותו) ----------

  /** ה-id של ה-Session. קיים גם כשעוד לא הגיעה תשובה (למשל בזמן טעינה אחרי רענון). */
  private readonly _sessionId = signal<string | null>(null);

  /** התשובה האחרונה מהשרת: זה מה שמוצג על המסך. */
  private readonly _session = signal<SessionDto | null>(null);

  /** האם יש עכשיו בקשה באוויר. בזמן הזה כל הכפתורים מושבתים. */
  private readonly _busy = signal(false);

  /** הודעת השגיאה האחרונה, אם יש. */
  private readonly _error = signal<string | null>(null);

  /** גודל העמוד שייבחר בכניסה הבאה ל"כל הקומבינציות". */
  private readonly _preferredPageSize = signal(DEFAULT_PAGE_SIZE);

  // ---------- מה שהרכיבים רואים (קריאה בלבד) ----------

  readonly sessionId = this._sessionId.asReadonly();
  readonly session = this._session.asReadonly();
  readonly busy = this._busy.asReadonly();
  readonly error = this._error.asReadonly();

  // ---------- הכפתורים ----------

  /** פעם אחת כשהמסך נפתח: אם נשמר id (רענון דף), שואלים את השרת מה המצב. */
  initialize(): void {
    const savedId = this.storage.read();
    if (savedId !== null) {
      this._sessionId.set(savedId);
      this.reload();
    }
  }

  /** "התחל". */
  start(n: number): void {
    if (this._sessionId() !== null) {
      return; // כבר יש Session; מתחילים מחדש רק אחרי איפוס
    }

    this.run(this.api.start(n), session => {
      this.adopt(session);
      this.storage.save(session.sessionId);
    });
  }

  /** "הבא". */
  next(): void {
    this.withSession(id => this.api.next(id));
  }

  /** "כל הקומבינציות": הכניסה היא תמיד לדף הראשון. */
  enterAll(): void {
    this.withSession(id => this.api.showAll(id, { action: 'First', pageSize: this._preferredPageSize() }));
  }

  /** כפתורי הדפדוף: ראשון, קודם, הבא, אחרון, מספר דף, קפיצה לדף. */
  navigate(navigation: PageNavigation): void {
    const page = this._session()?.page;
    if (!page) {
      return;
    }

    // גודל העמוד נשלח בכל קריאה, כמו שהמטלה דורשת
    this.showAll(navigation.action, page.pageSize, navigation.pageNumber);
  }

  /** שינוי גודל עמוד: השרת מחשב מחדש את העמוד שמכיל את השורה העליונה. */
  changePageSize(pageSize: number): void {
    this._preferredPageSize.set(pageSize);
    this.showAll('Current', pageSize);
  }

  /** "חזור". */
  back(): void {
    this.withSession(id => this.api.back(id));
  }

  /** "איפוס": מוחקים בשרת, ורק אחרי אישור מנקים גם כאן. */
  reset(): void {
    const id = this._sessionId();
    if (id === null) {
      return;
    }

    this.run(this.api.reset(id), () => this.clear());
  }

  /** טוען מחדש את המצב מהשרת. אחרי רענון דף, אחרי התנגשות, או בלחיצה על "נסה שוב". */
  reload(): void {
    this.withSession(id => this.api.getState(id));
  }

  // ---------- עזרים ----------

  private showAll(action: PageAction, pageSize: number, pageNumber?: string): void {
    this.withSession(id => this.api.showAll(id, { action, pageSize, pageNumber }));
  }

  /** בקשה שמחזירה את המצב החדש של ה-Session, ושמירתו כשהיא חוזרת. */
  private withSession(request: (sessionId: string) => Observable<SessionDto>): void {
    const id = this._sessionId();
    if (id === null) {
      return;
    }

    this.run(request(id), session => this.adopt(session));
  }

  /**
   * הלב של ה-Facade. כל הבקשות עוברות כאן:
   * 1. אם כבר יש בקשה באוויר, מתעלמים (מונע לחיצה כפולה).
   * 2. שולחים.
   * 3. בהצלחה: onSuccess. בכישלון: handleError.
   */
  private run<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    if (this._busy()) {
      return;
    }

    this._busy.set(true);
    this._error.set(null);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: value => {
        this._busy.set(false);
        onSuccess(value);
      },
      error: (error: unknown) => {
        this._busy.set(false);
        this.handleError(error);
      },
    });
  }

  /** מאמצים את התשובה כמו שהיא: היא כל מה שהמסך צריך. */
  private adopt(session: SessionDto): void {
    this._session.set(session);
    this._sessionId.set(session.sessionId);
  }

  /** חזרה למסך ההתחלה. */
  private clear(): void {
    this._session.set(null);
    this._sessionId.set(null);
    this.storage.remove();
  }

  private handleError(error: unknown): void {
    const uiError = toUiError(error);

    switch (uiError.code) {
      case 'SESSION_NOT_FOUND':
        // ה-Session פג או נמחק: חוזרים למסך ההתחלה
        this.clear();
        break;

      case 'SESSION_CONFLICT':
      case 'INVALID_MODE':
        // המסך לא מעודכן (למשל אותו Session פתוח בשתי לשוניות): טוענים את המצב האמיתי.
        // לא שולחים שוב את הפעולה עצמה, כדי לא לבצע אותה פעמיים.
        this.reload();
        break;
    }

    // אחרי reload, שמנקה הודעות קודמות, כדי שההודעה תישאר על המסך
    this._error.set(uiError.message);
  }
}
