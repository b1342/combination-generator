import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';
import { buttonByText, submitForm, text, typeInto } from '../../../../../testing/dom';
import { StartFormComponent } from './start-form.component';

describe('StartFormComponent', () => {
  // מאפסים את מודול הבדיקה אחרי כל בדיקה, כך שכל בדיקה מתחילה נקייה
  afterEach(() => TestBed.resetTestingModule());

  function create() {
    const fixture = TestBed.createComponent(StartFormComponent);
    const emitted: number[] = [];
    fixture.componentInstance.startRequested.subscribe(n => emitted.push(n));
    fixture.detectChanges();
    return { fixture, emitted };
  }

  it.each(['1', '3', '20'])('emits a valid n (%s) as a number', value => {
    const { fixture, emitted } = create();

    typeInto(fixture, '#n', value);
    submitForm(fixture);

    expect(emitted).toEqual([Number(value)]);
  });

  it.each(['', '0', '21', '2.5', '-3'])('does not emit for invalid input "%s" and shows an error', value => {
    const { fixture, emitted } = create();

    typeInto(fixture, '#n', value);
    submitForm(fixture);

    expect(emitted).toEqual([]);
    expect(text(fixture)).toContain('יש להזין מספר שלם בין 1 ל-20.');
    expect(fixture.nativeElement.querySelector('#n').getAttribute('aria-invalid')).toBe('true');
  });

  it('shows no error before the user touches the field', () => {
    const { fixture } = create();

    expect(text(fixture)).not.toContain('יש להזין מספר שלם');
    expect(fixture.nativeElement.querySelector('#n').getAttribute('aria-describedby')).toBeNull();
  });

  it('disables the button while a request is in flight', () => {
    const { fixture } = create();

    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    expect(buttonByText(fixture, 'התחל').disabled).toBe(true);
  });
});
