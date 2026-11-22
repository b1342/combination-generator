/**
 * הגדרות לפיתוח מקומי (ng serve, הבדיקות וה-E2E). מחליף את environment.ts דרך fileReplacements ב-angular.json.
 * השרת רץ בנפרד על פורט 5080, ולכן הכתובת מלאה, והשרת מתיר את localhost:4200 ב-appsettings.Development.json.
 */
export const environment = {
  /** הכתובת של ה-API, בלי "/" בסוף. */
  apiBaseUrl: 'http://localhost:5080/api/generators',
};
