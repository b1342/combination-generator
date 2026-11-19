import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SessionDto, ShowAllRequestDto } from './generator-api.models';

/**
 * שולח את הבקשות לשרת. פונקציה לכל כתובת ב-GeneratorsController, ותו לא:
 * לא זוכר מצב, לא מחליט כלום ולא מטפל בשגיאות. את זה עושה ה-Facade.
 */
@Injectable({ providedIn: 'root' })
export class GeneratorApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  /** "התחל": POST /api/generators */
  start(n: number): Observable<SessionDto> {
    return this.http.post<SessionDto>(this.baseUrl, { n });
  }

  /** המצב הנוכחי: GET /api/generators/{id}. משמש אחרי רענון דף. */
  getState(sessionId: string): Observable<SessionDto> {
    return this.http.get<SessionDto>(this.sessionUrl(sessionId));
  }

  /** "הבא": POST /api/generators/{id}/next */
  next(sessionId: string): Observable<SessionDto> {
    return this.http.post<SessionDto>(`${this.sessionUrl(sessionId)}/next`, null);
  }

  /** "כל הקומבינציות", דפדוף ושינוי גודל עמוד: POST /api/generators/{id}/all */
  showAll(sessionId: string, request: ShowAllRequestDto): Observable<SessionDto> {
    return this.http.post<SessionDto>(`${this.sessionUrl(sessionId)}/all`, request);
  }

  /** "חזור": POST /api/generators/{id}/back */
  back(sessionId: string): Observable<SessionDto> {
    return this.http.post<SessionDto>(`${this.sessionUrl(sessionId)}/back`, null);
  }

  /** "איפוס": DELETE /api/generators/{id}. השרת מחזיר 204 בלי גוף. */
  reset(sessionId: string): Observable<void> {
    return this.http.delete<void>(this.sessionUrl(sessionId));
  }

  private sessionUrl(sessionId: string): string {
    return `${this.baseUrl}/${encodeURIComponent(sessionId)}`;
  }
}
