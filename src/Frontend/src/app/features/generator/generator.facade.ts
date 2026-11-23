import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, timeout } from 'rxjs';
import { GeneratorApiService } from './data-access/generator-api.service';
import { PageAction, PageNavigation, SessionDto } from './data-access/generator-api.models';
import { toUiError } from './data-access/generator-errors';
import { SessionIdStorage } from './data-access/session-id-storage';
import { DEFAULT_PAGE_SIZE, REQUEST_TIMEOUT_MS } from './generator.config';

/** הגדרות לבקשה אחת. */
interface RunOptions {
  /** פעולה שמשנה מצב: אם לא ידוע אם השרת ביצע אותה, נכנסים ל"לא מסונכרן" וטוענים את המצב מחדש. */
  readonly recoverOnUnknown?: boolean;

  /** מותר גם כשהמסך לא מסונכרן. רק טעינת המצב ו"איפוס" מותרים אז. */
  readonly allowWhileOutOfSync?: boolean;

  /** טיפול מיוחד בשגיאה, במקום הטיפול הרגיל (handleError). */
  readonly onError?: (error: unknown) => void;

  /** זו טעינת המצב עצמה. אם היא נכשלת, לא מתחילים ממנה עוד טעינה, כדי שלא תיווצר לולאה. */
  readonly isReload?: boolean;
}

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

  /**
   * true כשלא ידוע אם המסך תואם לשרת: בקשה שמשנה מצב נכשלה בלי תשובה ברורה (למשל עבר הזמן).
   * במצב הזה כל הכפתורים חסומים עד שהטעינה מהשרת מצליחה, כדי שלחיצה על "הבא" לא תדלג על קומבינציה.
   */
  private readonly _outOfSync = signal(false);

  /** גודל העמוד שייבחר בכניסה הבאה ל"כל הקומבינציות". מתעדכן רק מתשובת השרת. */
  private readonly _preferredPageSize = signal(DEFAULT_PAGE_SIZE);

  // ---------- מה שהרכיבים רואים (קריאה בלבד) ----------

  readonly sessionId = this._sessionId.asReadonly();
  readonly session = this._session.asReadonly();
  readonly busy = this._busy.asReadonly();
  readonly error = this._error.asReadonly();
  readonly outOfSync = this._outOfSync.asReadonly();

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

    this.run(() => this.api.start(n), session => {
      this.adopt(session);
      this.storage.save(session.sessionId);
    });
  }

  /** "הבא". */
  next(): void {
    this.withSession(id => this.api.next(id));
  }

  /** "הקודם". */
  previous(): void {
    this.withSession(id => this.api.previous(id));
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
    // את הגודל המועדף לא משנים כאן אלא רק כשהשרת מאשר (adopt), כדי שגודל שנדחה לא יישאר
    this.showAll('Current', pageSize);
  }

  /** "חזור". */
  back(): void {
    this.withSession(id => this.api.back(id));
  }

  /**
   * "איפוס": מוחקים בשרת וחוזרים למסך ההתחלה.
   * גם אם המחיקה נכשלה חוזרים להתחלה, בלי הודעת שגיאה: המשתמשת ביקשה להתחיל מחדש, וזה מה שתקבל.
   * Session שלא נמחק בשרת יפוג לבד אחרי 30 דקות.
   */
  reset(): void {
    const id = this._sessionId();
    if (id === null) {
      return;
    }

    this.run(() => this.api.reset(id), () => this.clear(), {
      allowWhileOutOfSync: true,
      onError: () => this.clear(),
    });
  }

  /**
   * טוען מחדש את המצב מהשרת. אחרי רענון דף, אחרי התנגשות, או בלחיצה על "נסה שוב".
   * כשהטעינה מצליחה המסך שוב מסונכרן, ולכן גם הודעת השגיאה הקודמת כבר לא נכונה ונמחקת.
   */
  reload(): void {
    this.resync(true);
  }

  // ---------- עזרים ----------

  /**
   * טוען את המצב מהשרת. כשהטעינה מצליחה, המסך שוב מסונכרן והחסימה מוסרת.
   * clearErrorOnSuccess: האם למחוק גם את ההודעה. אחרי התנגשות משאירים אותה, כדי שהמשתמשת תבין למה המסך השתנה.
   */
  private resync(clearErrorOnSuccess: boolean): void {
    const id = this._sessionId();
    if (id === null) {
      return;
    }

    // recoverOnUnknown כבוי ו-isReload דלוק: אם גם הטעינה נכשלת, לא מנסים שוב לבד
    this.run(
      () => this.api.getState(id),
      session => {
        this.adopt(session);
        if (this._outOfSync()) {
          this._outOfSync.set(false);
          if (clearErrorOnSuccess) {
            this._error.set(null);
          }
        }
      },
      { allowWhileOutOfSync: true, isReload: true },
    );
  }

  private showAll(action: PageAction, pageSize: number, pageNumber?: string): void {
    this.withSession(id => this.api.showAll(id, { action, pageSize, pageNumber }));
  }

  /**
   * בקשה שמחזירה את המצב החדש של ה-Session, ושמירתו כשהיא חוזרת.
   * ברירת המחדל: פעולה שמשנה מצב, ולכן אם התוצאה לא ידועה טוענים את המצב מחדש.
   */
  private withSession(
    request: (sessionId: string) => Observable<SessionDto>,
    options: RunOptions = { recoverOnUnknown: true },
  ): void {
    const id = this._sessionId();
    if (id === null) {
      return;
    }

    this.run(() => request(id), session => this.adopt(session), options);
  }

  /**
   * הלב של ה-Facade. כל הבקשות עוברות כאן:
   * 1. אם כבר יש בקשה באוויר, מתעלמים (מונע לחיצה כפולה).
   *    גם כשהמסך לא מסונכרן מתעלמים, חוץ מטעינה ומאיפוס.
   * 2. רק אז יוצרים את הבקשה ושולחים אותה, עם הגבלת זמן.
   * 3. בהצלחה: onSuccess. בכישלון: onError אם הוגדר, אחרת handleError.
   * הבקשה מגיעה כפונקציה (createRequest) ולא כ-Observable מוכן, כדי שבלחיצה כפולה היא לא תיווצר בכלל.
   */
  private run<T>(
    createRequest: () => Observable<T>,
    onSuccess: (value: T) => void,
    options: RunOptions = {},
  ): void {
    if (this._busy() || (this._outOfSync() && !options.allowWhileOutOfSync)) {
      return;
    }

    this._busy.set(true);
    this._error.set(null);

    createRequest().pipe(timeout(REQUEST_TIMEOUT_MS), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: value => {
        this._busy.set(false);
        onSuccess(value);
      },
      error: (error: unknown) => {
        this._busy.set(false);
        if (options.onError) {
          options.onError(error);
        } else {
          this.handleError(error, options);
        }
      },
    });
  }

  /** מאמצים את התשובה כמו שהיא: היא כל מה שהמסך צריך. */
  private adopt(session: SessionDto): void {
    this._session.set(session);
    this._sessionId.set(session.sessionId);

    // הגודל שהשרת אישר הוא הגודל לכניסה הבאה ל"כל הקומבינציות"
    if (session.page) {
      this._preferredPageSize.set(session.page.pageSize);
    }
  }

  /** חזרה למסך ההתחלה. */
  private clear(): void {
    this._session.set(null);
    this._sessionId.set(null);
    this._preferredPageSize.set(DEFAULT_PAGE_SIZE);
    this._outOfSync.set(false);
    this._error.set(null);
    this.storage.remove();
  }

  private handleError(error: unknown, options: RunOptions): void {
    const uiError = toUiError(error);

    switch (uiError.code) {
      case 'SESSION_NOT_FOUND':
      case 'INVALID_SESSION_ID':
        // ה-Session פג, נמחק, או שה-id השמור פגום: חוזרים למסך ההתחלה
        this.clear();
        break;

      case 'SESSION_CONFLICT':
      case 'INVALID_MODE':
        // המסך לא מעודכן (למשל אותו Session פתוח בשתי לשוניות): חוסמים את הכפתורים וטוענים את המצב האמיתי.
        // לא שולחים שוב את הפעולה עצמה, כדי לא לבצע אותה פעמיים.
        // אם הטעינה תיכשל, החסימה נשארת ויופיע "נסה שוב", כדי שלחיצה לא תתקדם ממצב שלא מוצג.
        if (!options.isReload) {
          this._outOfSync.set(true);
          this.resync(false);
        }
        break;

      default:
        // אין תשובה, עבר הזמן, או שגיאת שרת: ייתכן שהשרת כן ביצע את הפעולה (למשל "הבא").
        // המסך כבר לא בטוח נכון, ולכן חוסמים את הכפתורים וטוענים את המצב האמיתי פעם אחת.
        // אם הטעינה תצליח, ההודעה והחסימה יוסרו; אם לא, יופיע "נסה שוב".
        if (options.recoverOnUnknown && uiError.outcomeUnknown && this._sessionId() !== null) {
          this._outOfSync.set(true);
          this.reload();
        }
        break;
    }

    // אם הטעינה כבר הצליחה (תשובה מיידית), המסך מסונכרן ואין מה להודיע
    if (options.recoverOnUnknown && uiError.outcomeUnknown && this._sessionId() !== null && !this._outOfSync()) {
      return;
    }

    // אחרי reload, שמנקה הודעות קודמות, כדי שההודעה תישאר על המסך
    this._error.set(uiError.message);
  }
}
