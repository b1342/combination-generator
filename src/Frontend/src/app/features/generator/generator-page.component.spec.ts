import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { SESSION_ID, allSession, combination, session } from '../../../testing/fixtures';
import { buttonByText, submitForm, text, typeInto, shownCombination } from '../../../testing/dom';
import { environment } from '../../../environments/environment';
import { GeneratorPageComponent } from './generator-page.component';

/**
 * בדיקת אינטגרציה של המסך כולו: רכיבים + Facade + ApiService אמיתיים.
 * רק השרת מדומה (HttpTestingController), כך שבודקים גם את הבקשות שנשלחות בפועל.
 */
describe('GeneratorPageComponent', () => {
  // מאפסים את מודול הבדיקה אחרי כל בדיקה, כך שכל בדיקה מתחילה נקייה
  afterEach(() => TestBed.resetTestingModule());

  // אותה כתובת שהשירות משתמש בה (בבדיקות: קובץ הפיתוח, דרך fileReplacements)
  const base = environment.apiBaseUrl;
  let http: HttpTestingController;
  let fixture: ComponentFixture<GeneratorPageComponent>;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function open(): void {
    fixture = TestBed.createComponent(GeneratorPageComponent);
    fixture.detectChanges();
  }

  function startWith(n: string, response = session()): void {
    typeInto(fixture, '#n', n);
    submitForm(fixture);
    http.expectOne(base).flush(response);
    fixture.detectChanges();
  }

  it('starts on the input screen, without any request', () => {
    open();

    expect(fixture.nativeElement.querySelector('#n')).not.toBeNull();
    expect(text(fixture)).not.toContain('איפוס');
  });

  it('start: shows n and the total with thousand separators', () => {
    open();

    startWith('20', session({ n: 20, totalCount: '2432902008176640000' }));

    // Angular מסיר רווחים בין תגיות, אז בודקים כל חלק בנפרד
    const labels = Array.from(fixture.nativeElement.querySelectorAll('.stat-label')).map(e => (e as HTMLElement).textContent?.trim());
    const values = Array.from(fixture.nativeElement.querySelectorAll('.stat-value')).map(e => (e as HTMLElement).textContent?.trim());
    expect(labels).toEqual(['מספר איברים', 'סה"כ האפשרויות לסדר 20 מספרים בשורה הוא:']);
    expect(values).toEqual(['20', '2,432,902,008,176,640,000']);
    expect(sessionStorage.getItem('combination-generator:sessionId')).toBe(SESSION_ID);
  });

  it('"next" shows the combination returned by the server', () => {
    open();
    startWith('3');

    buttonByText(fixture, 'הבא').click();
    http.expectOne(`${base}/${SESSION_ID}/next`).flush(session({ current: combination('1', [1, 2, 3]) }));
    fixture.detectChanges();

    expect(shownCombination(fixture)).toBe('קומבינציה מספר 1 – [1,2,3]');
  });

  it('"all combinations" switches to the table, "back" returns', () => {
    open();
    startWith('3');

    buttonByText(fixture, 'כל הקומבינציות').click();
    const entry = http.expectOne(`${base}/${SESSION_ID}/all`);
    expect(entry.request.body).toEqual({ action: 'First', pageSize: 10 }); // 6 קומבינציות: אין טעם ב-20
    entry.flush(allSession());
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();

    buttonByText(fixture, 'חזור').click();
    http.expectOne(`${base}/${SESSION_ID}/back`).flush(session({ current: combination('4', [2, 3, 1]) }));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    expect(shownCombination(fixture)).toBe('קומבינציה מספר 4 – [2,3,1]');
  });

  it('shows the server error message in an alert', () => {
    open();
    startWith('3');

    buttonByText(fixture, 'הבא').click();
    http
      .expectOne(`${base}/${SESSION_ID}/next`)
      .flush({ status: 409, code: 'INVALID_MODE', detail: 'הפעולה לא מתאימה למסך הנוכחי.' }, { status: 409, statusText: 'Conflict' });
    http.expectOne(`${base}/${SESSION_ID}`).flush(session());
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('הפעולה לא מתאימה למסך הנוכחי.');
  });

  it('when "next" gets no answer: locks the buttons until "try again" loads the real state', () => {
    open();
    startWith('3');

    buttonByText(fixture, 'הבא').click();
    http.expectOne(`${base}/${SESSION_ID}/next`).error(new ProgressEvent('error'));
    http.expectOne(`${base}/${SESSION_ID}`).error(new ProgressEvent('error'));
    fixture.detectChanges();

    expect(text(fixture)).toContain('לא ברור אם הפעולה האחרונה בוצעה');
    expect(buttonByText(fixture, 'הבא').disabled).toBe(true);
    expect(buttonByText(fixture, 'כל הקומבינציות').disabled).toBe(true);

    buttonByText(fixture, 'נסה שוב').click();
    http.expectOne(`${base}/${SESSION_ID}`).flush(session({ current: combination('1', [1, 2, 3]) }));
    fixture.detectChanges();

    expect(text(fixture)).not.toContain('לא ברור אם הפעולה האחרונה בוצעה');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(buttonByText(fixture, 'הבא').disabled).toBe(false);
    expect(shownCombination(fixture)).toBe('קומבינציה מספר 1 – [1,2,3]');
  });

  it('after "no more combinations": "next" is disabled, "previous" is not', () => {
    open();
    startWith('3');

    buttonByText(fixture, 'הבא').click();
    http.expectOne(`${base}/${SESSION_ID}/next`).flush(
      session({ current: combination('6', [3, 2, 1]), noMorePermutations: true, message: 'אין יותר קומבינציות.' }),
    );
    fixture.detectChanges();

    expect(buttonByText(fixture, 'הבא').disabled).toBe(true);
    expect(buttonByText(fixture, 'הקודם').disabled).toBe(false);
  });

  it('"reset" returns to the input screen even if the server did not answer', () => {
    open();
    startWith('3');

    buttonByText(fixture, 'איפוס').click();
    http.expectOne(`${base}/${SESSION_ID}`).error(new ProgressEvent('error'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('#n')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('"reset" returns to the input screen after the server confirms', () => {
    open();
    startWith('3');

    buttonByText(fixture, 'איפוס').click();
    http.expectOne(`${base}/${SESSION_ID}`).flush(null, { status: 204, statusText: 'No Content' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('#n')).not.toBeNull();
    expect(sessionStorage.getItem('combination-generator:sessionId')).toBeNull();
  });

  it('after a refresh: restores the state from the server with a GET', () => {
    sessionStorage.setItem('combination-generator:sessionId', SESSION_ID);
    open();
    expect(text(fixture)).toContain('טוען');

    const get = http.expectOne(`${base}/${SESSION_ID}`);
    expect(get.request.method).toBe('GET');
    get.flush(session({ current: combination('2', [1, 3, 2]) }));
    fixture.detectChanges();

    expect(shownCombination(fixture)).toBe('קומבינציה מספר 2 – [1,3,2]');
  });
});
