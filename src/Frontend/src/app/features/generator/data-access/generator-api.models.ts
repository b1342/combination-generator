// הצורה של ה-JSON שהשרת שולח ומקבל. בדיוק כמו Requests.cs ו-Responses.cs בשרת.
// מספרים שיכולים להיות גדולים (עד 20!) הם string: JavaScript מעגל מספרים מעל 2^53.

/** באיזה מסך ה-Session נמצא. */
export type SessionMode = 'Next' | 'All';

/** הכפתורים של "כל הקומבינציות". Current = אותו עמוד, למשל אחרי שינוי גודל עמוד. */
export type PageAction = 'First' | 'Previous' | 'Next' | 'Last' | 'Specific' | 'Current';

/** קומבינציה אחת: המספר שלה (מ-1) והתמורה. */
export interface CombinationDto {
  readonly number: string;
  readonly permutation: readonly number[];
}

/** כפתור אחד בשורת מספרי הדפים: מספר דף, או "…". */
export interface PageLinkDto {
  readonly type: 'page' | 'ellipsis';
  readonly number: string | null;
  readonly isCurrent: boolean;
}

/** עמוד בטבלה. */
export interface PageDto {
  readonly pageNumber: string;
  readonly totalPages: string;
  readonly pageSize: number;
  readonly rows: readonly CombinationDto[];
  readonly links: readonly PageLinkDto[];
}

/** התשובה של כל הכפתורים: מה המסך צריך להציג. */
export interface SessionDto {
  readonly sessionId: string;
  readonly n: number;
  readonly totalCount: string;
  readonly mode: SessionMode;
  readonly current: CombinationDto | null;
  readonly page: PageDto | null;
  readonly noMorePermutations: boolean;
  readonly message: string | null;
}

/** הגוף של POST .../all. pageNumber רק עבור Specific. */
export interface ShowAllRequestDto {
  readonly action: PageAction;
  readonly pageSize: number;
  readonly pageNumber?: string;
}

/** בקשת דפדוף מהמסך: איזה כפתור, ומספר דף אם זה Specific. */
export interface PageNavigation {
  readonly action: PageAction;
  readonly pageNumber?: string;
}
