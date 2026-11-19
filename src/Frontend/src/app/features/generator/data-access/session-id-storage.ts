import { Injectable } from '@angular/core';

/**
 * שומר בדפדפן רק את ה-sessionId, כדי שרענון דף לא יאבד את ה-Session.
 * את המצב עצמו לא שומרים: אחרי רענון שואלים את השרת (GET).
 * sessionStorage: נמחק כשסוגרים את הלשונית, ולכל לשונית יש משלה.
 */
@Injectable({ providedIn: 'root' })
export class SessionIdStorage {
  private static readonly Key = 'combination-generator:sessionId';
  private static readonly GuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  /** ה-id השמור, או null אם אין (או שהוא לא תקין). */
  read(): string | null {
    const value = this.tryAccess(() => sessionStorage.getItem(SessionIdStorage.Key));
    if (value === null || value === undefined) {
      return null;
    }

    if (!SessionIdStorage.GuidPattern.test(value)) {
      this.remove();
      return null;
    }

    return value;
  }

  save(sessionId: string): void {
    this.tryAccess(() => sessionStorage.setItem(SessionIdStorage.Key, sessionId));
  }

  remove(): void {
    this.tryAccess(() => sessionStorage.removeItem(SessionIdStorage.Key));
  }

  // דפדפן יכול לחסום גישה לאחסון (למשל במצב פרטיות מחמיר). אז פשוט ממשיכים בלי לשמור:
  // הכל עובד, רק רענון דף לא ישחזר את ה-Session.
  private tryAccess<T>(action: () => T): T | undefined {
    try {
      return action();
    } catch {
      return undefined;
    }
  }
}
