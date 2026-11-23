# מחולל הקומבינציות (Combination Generator)

אפליקציית Web, ‏Client/Server, שמחוללת את כל הסידורים (תמורות) של המספרים 1..n בשורה, עבור n בין 1 ל-20.

- **שרת:** ‏.NET 8 (ASP.NET Core), בארבע שכבות.
- **לקוח:** ‏Angular 20 (רכיבים standalone, ‏Signals).
- **כל החישובים נעשים בשרת.** הלקוח רק מציג את מה שהשרת מחזיר.

---

## דרישות מקדימות

| כלי | גרסה |
|---|---|
| ‏.NET SDK | 8.0 |
| ‏Node.js | ‏20.19 ומעלה, או 22.12 ומעלה |

> **Windows / PowerShell:** אם PowerShell חוסם את `npm` ("running scripts is disabled"), משתמשים ב-`npm.cmd` וב-`npx.cmd` במקום `npm` ו-`npx`.

---

## הרצה

### 1. השרת
```bash
dotnet run --project src/Backend/CombinationGenerator.Api --launch-profile http
```
- השרת עולה ב-`http://localhost:5080`.
- **Swagger** (בסביבת פיתוח): ‏`http://localhost:5080/swagger`. אפשר לנסות שם את כל ה-API מהדפדפן.
- אפשר גם לפתוח את `CombinationGenerator.sln` ב-Visual Studio ולהריץ את `CombinationGenerator.Api` עם F5.

### 2. הלקוח
בחלון טרמינל נוסף:
```bash
cd src/Frontend
npm install
npm start
```
ואז פותחים את `http://localhost:4200`.

השרת והלקוח צריכים לרוץ יחד.

---

## בדיקות

| סוג | כמות | פקודה |
|---|---|---|
| שרת: יחידה ואינטגרציה (xUnit) | 216 | `dotnet test` (מתיקיית הפרויקט הראשית) |
| לקוח: יחידה ורכיבים (Vitest + jsdom) | 102 | `cd src/Frontend` ואז `npm test` |
| קצה-לקצה בדפדפן (Playwright) | 11 | `cd src/Frontend` ואז `npm run e2e` |

**בדיקות השרת** כוללות בדיקות API שמריצות את השרת האמיתי בתוך הבדיקה (`WebApplicationFactory`): כל הזרימות, קודי שגיאה, מספרים גדולים, לחיצה כפולה, שלוש משתמשות במקביל ו-CORS.

**בדיקות הלקוח** רצות ב-Node, בלי דפדפן. `npm test` נשאר במצב מעקב; ‏`q` יוצא.

**בדיקות קצה-לקצה:** ‏Playwright מפעיל לבד את השרת ואת הלקוח, או משתמש בהם אם הם כבר רצים.
- **בפעם הראשונה** מורידים דפדפן לבדיקות: `npx playwright install chromium`
- **אם ההורדה חסומה** (למשל ברשת ארגונית), משתמשים ב-Edge שמותקן במחשב:
  ```powershell
  $env:PW_CHANNEL="msedge"; npm run e2e
  ```
- דוח מפורט, כולל צילומי מסך של כישלונות: `npx playwright show-report`

---

## ה-API

כתובת בסיס: `http://localhost:5080/api/generators`

| פעולה במסך | דרישת המטלה | בקשה | גוף | תשובה |
|---|---|---|---|---|
| התחל | **StartAPI** | `POST /` | `{ "n": 3 }` | ‏201 + המצב |
| הבא | **GetNextAPI** | `POST /{id}/next` | אין | ‏200 |
| הקודם (תוספת) | | `POST /{id}/previous` | אין | ‏200 |
| כל הקומבינציות ודפדוף | **GetAllAPI** | `POST /{id}/all` | `{ "action": "First", "pageSize": 20 }` | ‏200 |
| חזור | | `POST /{id}/back` | אין | ‏200 |
| המצב הנוכחי (אחרי רענון) | | `GET /{id}` | אין | ‏200 |
| איפוס | | `DELETE /{id}` | אין | ‏204 |

- **`action`:** ‏`First` (כניסה, ודף ראשון), ‏`Previous`, ‏`Next`, ‏`Last`, ‏`Specific` (עם `"pageNumber": "7"`), ו-`Current` (אותו דף, למשל אחרי שינוי גודל עמוד).
- **גודל העמוד נשלח בכל קריאה**, כמו שהמטלה דורשת. המקסימום בשרת: 200.
- **מספרים שיכולים להיות גדולים** (סה"כ, מספר קומבינציה, מספרי דפים) **נשלחים כטקסט**. ‏JavaScript מדייק רק עד 2^53, ו-20! גדול מזה.
- **שגיאות** חוזרות בפורמט ProblemDetails, עם שדה `code` (למשל `SESSION_NOT_FOUND`, ‏`NO_NEXT_PAGE`) והודעה בעברית בשדה `detail`.
- **בדיקת קלט קפדנית:** שדה חובה חסר (`n`, ‏`pageSize`) לא נחשב 0 אלא `INVALID_REQUEST`; מספר שנשלח כטקסט (`"n": "3"`) נדחה; ‏`pageNumber` מותר רק עם `Specific`, ובלי אפסים מובילים; ‏id שאינו GUID מחזיר 400 ‏`INVALID_SESSION_ID`.

---

## מבנה הפרויקט

```
src/
  Backend/
    CombinationGenerator.Domain          חוקים ואלגוריתמים. בלי תלויות.
      Permutations/   NextPermutation, PreviousPermutation, PermutationUnranker,
                      PermutationPageGenerator, FactorialCalculator
      Pagination/     PageCalculator, PageWindow
      Sessions/       GeneratorSession (immutable) ומעברי המצב
    CombinationGenerator.Application     GeneratorService: קריאה, חישוב, שמירה, תשובה
                                         ISessionStore, ‏PageLinksBuilder
    CombinationGenerator.Infrastructure  InMemorySessionStore, ‏SessionCleanupService
    CombinationGenerator.Api             Controller, חוזי JSON, שגיאות, ‏Program.cs
  Frontend/                              Angular 20
    src/app/features/generator/
      generator.facade.ts                המצב של המסך (Signals), בקשה אחת בכל פעם
      data-access/                       ApiService, מודלים, שגיאות, שמירת ה-id
      components/                        start-form, next-view, all-view, pagination
    e2e/                                 בדיקות Playwright
tests/                                   Domain, ‏Application, ‏Infrastructure, ‏Api
```

**כיוון התלויות:** ‏Api ← Application ← Domain, ו-Infrastructure מממשת את הממשק של Application. ‏Domain לא מכירה HTTP, אחסון או זמן.

---

## הגדרות

**שרת:** ‏`src/Backend/CombinationGenerator.Api/appsettings.json`, ובפיתוח גם `appsettings.Development.json` (שגובר עליו).

| מפתח | ברירת מחדל | משמעות |
|---|---|---|
| `SessionStore:IdleTimeoutMinutes` | ‏30 | אחרי כמה דקות בלי פעילות Session פג |
| `SessionStore:MaxSessions` | ‏1000 | כמה Sessions לכל היותר בו-זמנית |
| `SessionStore:CleanupIntervalSeconds` | ‏60 | כל כמה זמן המנקה ברקע מוחק Sessions שפגו |
| `Generator:MaxPageSize` | ‏200 | גודל עמוד מקסימלי |
| `Cors:AllowedOrigins` | ריק; בפיתוח `http://localhost:4200` | הכתובות שמורשות לפנות לשרת מהדפדפן. בפיתוח הלקוח והשרת רצים על פורטים שונים, ולכן צריך להתיר את הלקוח |

**לקוח:** כתובת ה-API נקבעת לפי סוג הבנייה:

| קובץ | מתי | כתובת |
|---|---|---|
| `src/environments/environment.development.ts` | ‏`ng serve`, הבדיקות וה-E2E (דרך `fileReplacements` ב-`angular.json`) | `http://localhost:5080/api/generators` |
| `src/environments/environment.ts` | ‏`ng build` (ייצור) | `/api/generators` (כתובת יחסית) |

**פריסה לייצור (לא חלק מהמטלה, ולא ממומש):** הייתי מגישה את קבצי `ng build` ואת ה-API מאותה כתובת, למשל מאחורי reverse proxy כמו nginx או IIS. בזכות הכתובת היחסית לא צריך אז CORS בכלל, ולכן ברירת המחדל בשרת היא רשימה ריקה.

הממשק משתמש בגופני המערכת בלבד, בלי הורדה מאתר חיצוני, כך שהוא עובד גם בלי אינטרנט.

---

## תשובות לשאלות הכלליות

### 1. מודל ניהול ה-State
**State בצד השרת, Session לכל משתמש.** כל "התחל" יוצר Session עם מזהה (GUID), והלקוח שומר רק את המזהה (ב-sessionStorage, כדי שרענון דף לא יאבד אותו).

- **מה נשמר:** רק מה שצריך כדי לחשב את הצעד הבא: n, והמצב, שהוא אחד משניים:
  - מצב "הבא": האינדקס והתמורה האחרונה שהוצגה.
  - מצב "כל הקומבינציות": מאיפה הטבלה מתחילה, גודל העמוד ומספר הדף.

  כלומר O(n) זיכרון לכל משתמש. **אף פעם לא רשימת תמורות.**
- **`GeneratorSession` הוא immutable:** כל פעולה מחזירה מצב חדש, ואת ההחלטה אם לשמור אותו מקבל ה-Service. לכן קל לבדוק את החוקים, ואין מצבי ביניים שבורים.
- **ה-Service חסר מצב** (Singleton): כל המצב נמצא במאגר, מאחורי `ISessionStore`.
- **גרסה לכל Session:** שמירה מצליחה רק אם הגרסה לא השתנתה מאז הקריאה (compare-and-swap).

**למה:** המטלה דורשת ש-GetNextAPI "יזכור" את הקריאה הקודמת, ושכל החישוב יהיה בשרת. State בשרת, מאחורי ממשק, מאפשר גם להחליף את האחסון (למשל Redis) בלי לגעת בלוגיקה.

### 2. NextPermutation מול Index-Based Permutation
| | NextPermutation | Index-Based (Unrank) |
|---|---|---|
| **מה עושה** | מהתמורה הנוכחית לבאה אחריה | מחשבת ישירות את התמורה במקום k |
| **צריך** | את התמורה הקודמת | רק את k (ואת n) |
| **זמן** | O(n), בממוצע O(1) | O(n²), עם בסיס העצרת (factorial number system) |

**איפה כל אחד:**
- **"הבא"** ‏← NextPermutation, כי התמורה הקודמת כבר שמורה, וזה הכי זול.
- **עמוד ב"כל הקומבינציות"** ← Unrank לשורה הראשונה של העמוד, כדי לקפוץ ישר לכל עמוד, גם לאחרון. אחר כך NextPermutation לשאר השורות בעמוד.
- **"חזור"** ← Unrank לשורה האחרונה בעמוד.
- **"הקודם"** (תוספת) ← PreviousPermutation, תמונת מראה של NextPermutation.

### 3. שלוש משתמשות בו-זמנית
כל אחת לוחצת "התחל" ומקבלת Session משלה, עם מזהה משלה. המצבים נשמרים ב-`ConcurrentDictionary` ולא נוגעים זה בזה, וה-Service חסר מצב, כך שהבקשות רצות במקביל. קריאה ועדכון לא נועלים בכלל (העדכון הוא compare-and-swap); רק יצירת Session חדש נעולה לרגע קצר, כדי שבדיקת התקרה של 1000 תהיה מדויקת.

אם **אותה** משתמשת שולחת שתי בקשות יחד (לחיצה כפולה, או שתי לשוניות על אותו Session), רק אחת נשמרת. השנייה מקבלת ‏`409 SESSION_CONFLICT`, והלקוח טוען את המצב העדכני. אין דילוג על קומבינציה ואין כפילות.

יש לזה בדיקות: `ThreeUsersAtOnce_EachKeepsHerOwnState` ובדיקת Playwright של שתי לשוניות שכל אחת מקבלת Session משלה (הפרדה בין משתמשות), ו-`DoubleClick_OnSameSession_NeverSkipsOrRepeatsACombination` (התנגשות באותו Session).

### 4. מעבר ל-Redis: שני שינויים בשרת
1. **מימוש חדש של `ISessionStore`, ‏`RedisSessionStore`:**
   - ה-Session נשמר כ-JSON תחת המפתח `session:{id}`.
   - התפוגה היא TTL של Redis (`EXPIRE`, מתחדש בכל גישה).
   - "החלף רק אם הגרסה לא השתנתה" נעשה בפעולה אטומית אחת, עם סקריפט Lua (או `WATCH`/`MULTI`).
2. **רישום ב-DI:** ב-`Program.cs` מחליפים את `AddInMemorySessionStore(...)` ב-`AddRedisSessionStore(...)`, עם מחרוזת חיבור מההגדרות. המנקה ברקע כבר לא נחוץ, כי Redis מוחק לבד מפתחות שפג תוקפם.

‏Domain, ‏Application וה-Controller לא משתנים.

### 5. החיסרון העיקרי של NextPermutation עבור n = 20
**הוא סדרתי:** כדי להגיע לתמורה k צריך לעבור על כל k התמורות שלפניה. ל-n=20 יש ‏20! ≈ 2.43×10¹⁸ תמורות. גם במיליארד צעדים בשנייה, הגעה לעמוד האחרון הייתה לוקחת **כ-77 שנים**. בנוסף, הוא תלוי בתמורה הקודמת, ולכן מחייב לשמור אותה.

בגלל זה הקפיצה לעמוד (ובמיוחד ל"אחרון") נעשית ב-Unrank, שמגיע לכל מקום בעשרות פעולות.
