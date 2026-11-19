import { describe, expect, it } from 'vitest';
import { GroupDigitsPipe } from './group-digits.pipe';

describe('GroupDigitsPipe', () => {
  const pipe = new GroupDigitsPipe();

  it.each([
    ['0', '0'],
    ['6', '6'],
    ['120', '120'],
    ['1000', '1,000'],
    ['720720', '720,720'],
    ['2432902008176640000', '2,432,902,008,176,640,000'],
    ['2432902008176639999', '2,432,902,008,176,639,999'], // מעל 2^53: כל ספרה נשמרת, בלי עיגול
  ])('%s → %s', (value, expected) => {
    expect(pipe.transform(value)).toBe(expected);
  });

  it('returns an empty string for null, undefined or empty', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
    expect(pipe.transform('')).toBe('');
  });
});
