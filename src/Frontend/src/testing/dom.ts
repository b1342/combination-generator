import { ComponentFixture } from '@angular/core/testing';

// עזרים קטנים לבדיקות של רכיבים: למצוא כפתור לפי הטקסט שלו, ולהקליד בשדה.

/** כל fixture של רכיב, בלי קשר לסוג הרכיב. */
type Fixture = Pick<ComponentFixture<unknown>, 'nativeElement' | 'detectChanges'>;

export function buttonByText(fixture: Fixture, text: string): HTMLButtonElement {
  const element = fixture.nativeElement as HTMLElement;
  const button = Array.from(element.querySelectorAll('button')).find(b => b.textContent?.trim() === text);
  if (!button) {
    throw new Error(`No button with text "${text}"`);
  }

  return button;
}

export function typeInto(fixture: Fixture, selector: string, value: string): void {
  const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(selector);
  if (!input) {
    throw new Error(`No input matches "${selector}"`);
  }

  input.value = value;
  input.dispatchEvent(new Event('input'));
}

export function submitForm(fixture: Fixture): void {
  (fixture.nativeElement as HTMLElement).querySelector('form')?.dispatchEvent(new Event('submit'));
  fixture.detectChanges();
}

export function text(fixture: Fixture): string {
  return ((fixture.nativeElement as HTMLElement).textContent ?? '').replace(/\s+/g, ' ').trim();
}

/** מה שמוצג במסך "הבא", בנוסח של המטלה: "קומבינציה מספר 1 – [1,2,3]" (השורה + הריבועים). */
export function shownCombination(fixture: Fixture): string {
  const element = fixture.nativeElement as HTMLElement;
  const line = (element.querySelector('.current-line')?.textContent ?? '').replace(/\s+/g, ' ').trim();
  const values = Array.from(element.querySelectorAll('.tile')).map(tile => tile.textContent?.trim());
  return `${line} – [${values.join(',')}]`;
}
