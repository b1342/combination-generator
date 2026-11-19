import { CombinationDto, PageDto, SessionDto } from '../app/features/generator/data-access/generator-api.models';

// נתוני דוגמה לבדיקות. כל פונקציה מחזירה אובייקט תקין, ואפשר לשנות בו רק את מה שהבדיקה צריכה.

export const SESSION_ID = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

export function combination(number: string, permutation: number[]): CombinationDto {
  return { number, permutation };
}

export function page(overrides: Partial<PageDto> = {}): PageDto {
  return {
    pageNumber: '1',
    totalPages: '2',
    pageSize: 2,
    rows: [combination('3', [2, 1, 3]), combination('4', [2, 3, 1])],
    links: [
      { type: 'page', number: '1', isCurrent: true },
      { type: 'page', number: '2', isCurrent: false },
    ],
    ...overrides,
  };
}

export function session(overrides: Partial<SessionDto> = {}): SessionDto {
  return {
    sessionId: SESSION_ID,
    n: 3,
    totalCount: '6',
    mode: 'Next',
    current: null,
    page: null,
    noMorePermutations: false,
    message: null,
    ...overrides,
  };
}

export function allSession(pageOverrides: Partial<PageDto> = {}): SessionDto {
  return session({ mode: 'All', page: page(pageOverrides) });
}
