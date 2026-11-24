import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Subject, TimeoutError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { SESSION_ID, allSession, combination, session } from '../../../testing/fixtures';
import { GeneratorApiService } from './data-access/generator-api.service';
import { SessionDto } from './data-access/generator-api.models';
import { SessionIdStorage } from './data-access/session-id-storage';
import { GeneratorFacade } from './generator.facade';

// במקום שרת: כל פונקציה ב-API מחזירה Subject, והבדיקה מחליטה מתי ומה "השרת" עונה.
function createApi() {
  return {
    start: vi.fn(),
    getState: vi.fn(),
    next: vi.fn(),
    previous: vi.fn(),
    showAll: vi.fn(),
    back: vi.fn(),
    reset: vi.fn(),
  };
}

type ApiMock = ReturnType<typeof createApi>;

function createStorage(savedId: string | null) {
  return { read: vi.fn(() => savedId), save: vi.fn(), remove: vi.fn() };
}

/** הבקשה הבאה לפונקציה הזו תחזיר Subject חדש; מחזירים אותו כדי לענות לו. */
function pending<T>(method: ApiMock[keyof ApiMock]): Subject<T> {
  const subject = new Subject<T>();
  method.mockReturnValueOnce(subject);
  return subject;
}

function respond<T>(subject: Subject<T>, value: T): void {
  subject.next(value);
  subject.complete();
}

function problem(status: number, code: string, detail = 'הודעה מהשרת'): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: { status, code, detail } });
}

describe('GeneratorFacade', () => {
  // מאפסים את מודול הבדיקה אחרי כל בדיקה, כך שכל בדיקה מתחילה נקייה
  afterEach(() => TestBed.resetTestingModule());

  let api: ApiMock;
  let storage: ReturnType<typeof createStorage>;
  let facade: GeneratorFacade;

  function setup(savedId: string | null = null): void {
    api = createApi();
    storage = createStorage(savedId);

    TestBed.configureTestingModule({
      providers: [
        GeneratorFacade,
        { provide: GeneratorApiService, useValue: api },
        { provide: SessionIdStorage, useValue: storage },
      ],
    });
    facade = TestBed.inject(GeneratorFacade);
  }

  /** מגיעים למצב שיש בו Session, דרך "התחל". */
  function startWith(state: SessionDto = session()): void {
    const start = pending<SessionDto>(api.start);
    facade.start(state.n);
    respond(start, state);
  }

  // ---------- פתיחה ורענון ----------

  describe('initialize', () => {
    it('without a saved id: sends nothing and shows the start screen', () => {
      setup(null);

      facade.initialize();

      expect(api.getState).not.toHaveBeenCalled();
      expect(facade.session()).toBeNull();
      expect(facade.sessionId()).toBeNull();
    });

    it('with a saved id (page refresh): asks the server for the state', () => {
      setup(SESSION_ID);
      const get = pending<SessionDto>(api.getState);

      facade.initialize();
      expect(facade.busy()).toBe(true);
      respond(get, session({ current: combination('2', [1, 3, 2]) }));

      expect(api.getState).toHaveBeenCalledWith(SESSION_ID);
      expect(facade.session()?.current?.number).toBe('2');
      expect(facade.busy()).toBe(false);
    });

    it('with a saved id that expired: returns to the start screen and forgets it', () => {
      setup(SESSION_ID);
      const get = pending<SessionDto>(api.getState);

      facade.initialize();
      get.error(problem(404, 'SESSION_NOT_FOUND'));

      expect(facade.sessionId()).toBeNull();
      expect(storage.remove).toHaveBeenCalled();
      expect(facade.error()).toBe('הודעה מהשרת');
    });
  });

  // ---------- התחל ----------

  describe('start', () => {
    it('adopts the response and saves only the id', () => {
      setup();

      startWith(session({ n: 20, totalCount: '2432902008176640000' }));

      expect(api.start).toHaveBeenCalledWith(20);
      expect(facade.session()?.totalCount).toBe('2432902008176640000');
      expect(storage.save).toHaveBeenCalledWith(SESSION_ID);
    });

    it('is ignored while a session exists', () => {
      setup();
      startWith();

      facade.start(5);

      expect(api.start).toHaveBeenCalledTimes(1);
    });

    it('on failure stays on the start screen with the message', () => {
      setup();
      const start = pending<SessionDto>(api.start);

      facade.start(3);
      start.error(problem(503, 'CAPACITY_REACHED', 'השרת עמוס'));

      expect(facade.session()).toBeNull();
      expect(facade.error()).toBe('השרת עמוס');
      expect(storage.save).not.toHaveBeenCalled();
    });
  });

  // ---------- בקשה אחת בכל פעם ----------

  it('ignores a second click while a request is in flight', () => {
    setup();
    startWith();
    const next = pending<SessionDto>(api.next);

    facade.next();
    facade.next();
    facade.previous();
    facade.reset();

    expect(api.next).toHaveBeenCalledTimes(1);
    expect(api.previous).not.toHaveBeenCalled();
    expect(api.reset).not.toHaveBeenCalled();
    respond(next, session({ current: combination('1', [1, 2, 3]) }));
    expect(facade.busy()).toBe(false);
  });

  // ---------- הבא / הקודם / חזור ----------

  const navigationButtons: Array<['next' | 'previous' | 'back', (f: GeneratorFacade) => void]> = [
    ['next', f => f.next()],
    ['previous', f => f.previous()],
    ['back', f => f.back()],
  ];

  for (const [method, action] of navigationButtons) {
    it(`${method}: sends the session id and adopts the new state`, () => {
      setup();
      startWith();
      const request = pending<SessionDto>(api[method]);

      action(facade);
      respond(request, session({ current: combination('4', [2, 3, 1]) }));

      expect(api[method]).toHaveBeenCalledWith(SESSION_ID);
      expect(facade.session()?.current).toEqual(combination('4', [2, 3, 1]));
    });
  }

  it('shows the "no more" message that the server sends', () => {
    setup();
    startWith();
    const next = pending<SessionDto>(api.next);

    facade.next();
    respond(next, session({ noMorePermutations: true, message: 'אין יותר קומבינציות', current: combination('6', [3, 2, 1]) }));

    expect(facade.session()?.message).toBe('אין יותר קומבינציות');
  });

  // ---------- כל הקומבינציות ----------

  describe('all combinations', () => {
    it('with 6 combinations, enters with the smallest size that shows them all (10, not 20)', () => {
      setup();
      startWith();
      const all = pending<SessionDto>(api.showAll);

      facade.enterAll();
      respond(all, allSession());

      // יש רק 6 קומבינציות: במקום 20, הגודל הקטן שכבר מציג את כולן
      expect(api.showAll).toHaveBeenCalledWith(SESSION_ID, { action: 'First', pageSize: 10 });
      expect(facade.session()?.mode).toBe('All');
    });

    it('with many combinations, enters with the default page size', () => {
      setup();
      startWith(session({ n: 20, totalCount: '2432902008176640000' }));
      const all = pending<SessionDto>(api.showAll);

      facade.enterAll();
      respond(all, allSession());

      expect(api.showAll).toHaveBeenCalledWith(SESSION_ID, { action: 'First', pageSize: 20 });
    });

    it('navigates with the page size of the current page, page number as a string', () => {
      setup();
      startWith(allSession({ pageSize: 50 }));
      const all = pending<SessionDto>(api.showAll);

      facade.navigate({ action: 'Specific', pageNumber: '12164510040883200' });
      respond(all, allSession({ pageSize: 50 }));

      expect(api.showAll).toHaveBeenCalledWith(SESSION_ID, {
        action: 'Specific',
        pageSize: 50,
        pageNumber: '12164510040883200',
      });
    });

    it('does not navigate when no page is shown', () => {
      setup();
      startWith();

      facade.navigate({ action: 'Next' });

      expect(api.showAll).not.toHaveBeenCalled();
    });

    it('changing page size sends Current; the accepted size is used on the next entry', () => {
      setup();
      startWith(allSession({ pageSize: 20 }));
      const resize = pending<SessionDto>(api.showAll);

      facade.changePageSize(5);
      respond(resize, allSession({ pageSize: 5 }));
      expect(api.showAll).toHaveBeenLastCalledWith(SESSION_ID, { action: 'Current', pageSize: 5 });

      const entry = pending<SessionDto>(api.showAll);
      facade.enterAll();
      respond(entry, allSession({ pageSize: 5 }));
      expect(api.showAll).toHaveBeenLastCalledWith(SESSION_ID, { action: 'First', pageSize: 5 });
    });

    it('a rejected page size is not remembered', () => {
      setup();
      startWith(allSession({ pageSize: 20 }));
      const resize = pending<SessionDto>(api.showAll);

      facade.changePageSize(5);
      resize.error(problem(400, 'INVALID_PAGE_SIZE'));

      const entry = pending<SessionDto>(api.showAll);
      facade.enterAll();
      respond(entry, allSession());
      expect(api.showAll).toHaveBeenLastCalledWith(SESSION_ID, { action: 'First', pageSize: 10 });
    });

    it('an edge error (no next page) keeps the page and shows the message', () => {
      setup();
      startWith(allSession());
      const all = pending<SessionDto>(api.showAll);

      facade.navigate({ action: 'Next' });
      all.error(problem(409, 'NO_NEXT_PAGE', 'זה כבר הדף האחרון.'));

      expect(facade.session()?.mode).toBe('All');
      expect(facade.error()).toBe('זה כבר הדף האחרון.');
      expect(api.getState).not.toHaveBeenCalled();
    });
  });

  // ---------- שגיאות והתאוששות ----------

  describe('errors and recovery', () => {
    it('a conflict reloads the real state once, without resending the action', () => {
      setup();
      startWith();
      const next = pending<SessionDto>(api.next);
      const get = pending<SessionDto>(api.getState);

      facade.next();
      next.error(problem(409, 'SESSION_CONFLICT', 'המצב השתנה'));
      respond(get, session({ current: combination('3', [2, 1, 3]) }));

      expect(api.next).toHaveBeenCalledTimes(1);
      expect(api.getState).toHaveBeenCalledTimes(1);
      expect(facade.session()?.current?.number).toBe('3');
      expect(facade.error()).toBe('המצב השתנה');
    });

    it('conflict followed by a failed reload: stays locked until "try again" succeeds', () => {
      setup();
      startWith();
      const next = pending<SessionDto>(api.next);
      const get = pending<SessionDto>(api.getState);

      facade.next();
      next.error(problem(409, 'SESSION_CONFLICT', 'המצב השתנה'));
      get.error(new HttpErrorResponse({ status: 0 }));

      // המסך מציג מצב ישן: "הבא" היה מתקדם ממצב שהמשתמשת לא רואה, ולכן חסום
      expect(facade.outOfSync()).toBe(true);
      facade.next();
      expect(api.next).toHaveBeenCalledTimes(1);

      const retry = pending<SessionDto>(api.getState);
      facade.reload();
      respond(retry, session({ current: combination('3', [2, 1, 3]) }));

      expect(facade.outOfSync()).toBe(false);
      expect(facade.error()).toBeNull();
      expect(facade.session()?.current?.number).toBe('3');
    });

    it('a stored id that is not valid (INVALID_SESSION_ID) returns to the start screen', () => {
      setup('not-a-guid');
      const get = pending<SessionDto>(api.getState);

      facade.initialize();
      get.error(problem(400, 'INVALID_SESSION_ID'));

      expect(facade.sessionId()).toBeNull();
      expect(storage.remove).toHaveBeenCalled();
    });

    it('a session that disappeared returns to the start screen', () => {
      setup();
      startWith();
      const next = pending<SessionDto>(api.next);

      facade.next();
      next.error(problem(404, 'SESSION_NOT_FOUND'));

      expect(facade.session()).toBeNull();
      expect(facade.sessionId()).toBeNull();
      expect(storage.remove).toHaveBeenCalled();
    });

    const unknownOutcomes: Array<[string, unknown]> = [
      ['no response', new HttpErrorResponse({ status: 0 })],
      ['server error', new HttpErrorResponse({ status: 500, error: { status: 500 } })],
      ['timeout', new TimeoutError()],
    ];

    it.each(unknownOutcomes)('unknown outcome (%s) after "next": reloads the state once', (_, error) => {
      setup();
      startWith();
      const next = pending<SessionDto>(api.next);
      const get = pending<SessionDto>(api.getState);

      facade.next();
      next.error(error);
      // השרת כן ביצע את "הבא": המסך מתעדכן לקומבינציה 1, ולא ידלג עליה בלחיצה הבאה
      respond(get, session({ current: combination('1', [1, 2, 3]) }));

      expect(api.getState).toHaveBeenCalledTimes(1);
      expect(facade.session()?.current?.number).toBe('1');
      // המסך שוב תואם לשרת: אין חסימה ואין הודעה ישנה
      expect(facade.outOfSync()).toBe(false);
      expect(facade.error()).toBeNull();
    });

    it('while the outcome is unknown, actions are blocked until the state is loaded', () => {
      setup();
      startWith();
      const next = pending<SessionDto>(api.next);
      const get = pending<SessionDto>(api.getState);

      facade.next();
      next.error(new HttpErrorResponse({ status: 0 }));
      get.error(new HttpErrorResponse({ status: 0 }));

      expect(facade.outOfSync()).toBe(true);

      // כל לחיצה כזו הייתה עלולה לדלג על קומבינציה, ולכן לא נשלחת
      facade.next();
      facade.previous();
      facade.enterAll();
      facade.back();
      expect(api.next).toHaveBeenCalledTimes(1);
      expect(api.previous).not.toHaveBeenCalled();
      expect(api.showAll).not.toHaveBeenCalled();
      expect(api.back).not.toHaveBeenCalled();

      // "נסה שוב" כן מותר, ואחרי שהוא מצליח הכול חוזר לעבוד
      const retry = pending<SessionDto>(api.getState);
      facade.reload();
      respond(retry, session({ current: combination('1', [1, 2, 3]) }));

      expect(facade.outOfSync()).toBe(false);
      expect(facade.error()).toBeNull();

      pending<SessionDto>(api.next);
      facade.next();
      expect(api.next).toHaveBeenCalledTimes(2);
    });

    it('if the reload also fails, it does not retry again (no loop)', () => {
      setup();
      startWith();
      const next = pending<SessionDto>(api.next);
      const get = pending<SessionDto>(api.getState);

      facade.next();
      next.error(new HttpErrorResponse({ status: 0 }));
      get.error(new HttpErrorResponse({ status: 0 }));

      expect(api.getState).toHaveBeenCalledTimes(1);
      expect(facade.busy()).toBe(false);
      expect(facade.error()).toContain('לא ניתן להתחבר לשרת');
      expect(facade.session()).not.toBeNull(); // התצוגה האחרונה נשארת
    });
  });

  // ---------- איפוס ----------

  describe('reset', () => {
    it('clears everything only after the server confirms', () => {
      setup();
      startWith();
      const reset = pending<void>(api.reset);

      facade.reset();
      expect(facade.session()).not.toBeNull(); // עוד לא אושר

      reset.next();
      reset.complete();

      expect(api.reset).toHaveBeenCalledWith(SESSION_ID);
      expect(facade.session()).toBeNull();
      expect(facade.sessionId()).toBeNull();
      expect(storage.remove).toHaveBeenCalled();
    });

    it.each([
      ['no response', new HttpErrorResponse({ status: 0 })],
      ['session already gone', problem(404, 'SESSION_NOT_FOUND')],
      ['server error', new HttpErrorResponse({ status: 500, error: { status: 500 } })],
    ])('on failure (%s) still returns to the start screen, without an error', (_, error) => {
      setup();
      startWith();
      const reset = pending<void>(api.reset);

      facade.reset();
      reset.error(error);

      expect(api.getState).not.toHaveBeenCalled();
      expect(facade.session()).toBeNull();
      expect(facade.sessionId()).toBeNull();
      expect(facade.error()).toBeNull();
      expect(storage.remove).toHaveBeenCalled();
    });

    it('is allowed while out of sync, and clears the lock', () => {
      setup();
      startWith();
      const next = pending<SessionDto>(api.next);
      const get = pending<SessionDto>(api.getState);
      facade.next();
      next.error(new TimeoutError());
      get.error(new TimeoutError());
      expect(facade.outOfSync()).toBe(true);

      const reset = pending<void>(api.reset);
      facade.reset();
      reset.next();
      reset.complete();

      expect(facade.sessionId()).toBeNull();
      expect(facade.outOfSync()).toBe(false);
    });
  });
});
