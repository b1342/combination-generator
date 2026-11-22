import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { SESSION_ID, session } from '../../../../testing/fixtures';
import { environment } from '../../../../environments/environment';
import { GeneratorApiService } from './generator-api.service';

describe('GeneratorApiService', () => {
  // מאפסים את מודול הבדיקה אחרי כל בדיקה, כך שכל בדיקה מתחילה נקייה
  afterEach(() => TestBed.resetTestingModule());

  // אותה כתובת שהשירות משתמש בה (בבדיקות: קובץ הפיתוח, דרך fileReplacements)
  const base = environment.apiBaseUrl;
  let api: GeneratorApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(GeneratorApiService);
    http = TestBed.inject(HttpTestingController);
  });

  // אחרי כל בדיקה: לא נשארה בקשה שלא טופלה, ולא נשלחה בקשה מיותרת
  afterEach(() => http.verify());

  it('start: POST to the base URL with { n }', () => {
    api.start(3).subscribe();

    const request = http.expectOne(base);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ n: 3 });
    request.flush(session());
  });

  it('getState: GET without a body', () => {
    api.getState(SESSION_ID).subscribe();

    const request = http.expectOne(`${base}/${SESSION_ID}`);
    expect(request.request.method).toBe('GET');
    expect(request.request.body).toBeNull();
    request.flush(session());
  });

  it.each([
    ['next', (id: string, service: GeneratorApiService) => service.next(id)],
    ['previous', (id: string, service: GeneratorApiService) => service.previous(id)],
    ['back', (id: string, service: GeneratorApiService) => service.back(id)],
  ])('%s: POST to /{id}/<action> without a body', (path, call) => {
    call(SESSION_ID, api).subscribe();

    const request = http.expectOne(`${base}/${SESSION_ID}/${path}`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush(session());
  });

  it('showAll: POST to /{id}/all with action, page size and page number as a string', () => {
    api.showAll(SESSION_ID, { action: 'Specific', pageSize: 20, pageNumber: '12164510040883200' }).subscribe();

    const request = http.expectOne(`${base}/${SESSION_ID}/all`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ action: 'Specific', pageSize: 20, pageNumber: '12164510040883200' });
    request.flush(session());
  });

  it('reset: DELETE /{id} and completes on 204', () => {
    let completed = false;
    api.reset(SESSION_ID).subscribe({ complete: () => (completed = true) });

    const request = http.expectOne(`${base}/${SESSION_ID}`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(completed).toBe(true);
  });

  it('returns the response exactly as received, big numbers stay strings', () => {
    let received: unknown;
    api.start(20).subscribe(value => (received = value));

    http.expectOne(base).flush(session({ n: 20, totalCount: '2432902008176640000' }));

    expect(received).toEqual(session({ n: 20, totalCount: '2432902008176640000' }));
  });
});
