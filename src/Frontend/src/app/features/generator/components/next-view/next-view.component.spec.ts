import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';
import { combination } from '../../../../../testing/fixtures';
import { buttonByText, text, shownCombination } from '../../../../../testing/dom';
import { CombinationDto } from '../../data-access/generator-api.models';
import { NextViewComponent } from './next-view.component';

describe('NextViewComponent', () => {
  // מאפסים את מודול הבדיקה אחרי כל בדיקה, כך שכל בדיקה מתחילה נקייה
  afterEach(() => TestBed.resetTestingModule());

  function create(current: CombinationDto | null, disabled = false) {
    const fixture = TestBed.createComponent(NextViewComponent);
    fixture.componentRef.setInput('current', current);
    fixture.componentRef.setInput('disabled', disabled);

    const events: string[] = [];
    fixture.componentInstance.previousRequested.subscribe(() => events.push('previous'));
    fixture.componentInstance.nextRequested.subscribe(() => events.push('next'));
    fixture.componentInstance.allRequested.subscribe(() => events.push('all'));

    fixture.detectChanges();
    return { fixture, events };
  }

  it('before the first click: shows a hint, and "previous" is disabled', () => {
    const { fixture } = create(null);

    expect(text(fixture)).toContain('לחצו על "הבא" להצגת הקומבינציה הראשונה');
    expect(buttonByText(fixture, 'הקודם').disabled).toBe(true);
  });

  it('shows the combination in the assignment format', () => {
    const { fixture } = create(combination('2', [1, 3, 2]));

    expect(shownCombination(fixture)).toBe('קומבינציה מספר 2 – [1,3,2]');
  });

  it('groups digits of a huge serial number without rounding', () => {
    const { fixture } = create(combination('2432902008176639999', [20, 19]));

    expect(text(fixture)).toContain('קומבינציה מספר 2,432,902,008,176,639,999');
  });

  it('"previous" is disabled on combination 1 and enabled after it', () => {
    expect(buttonByText(create(combination('1', [1, 2, 3])).fixture, 'הקודם').disabled).toBe(true);
    expect(buttonByText(create(combination('2', [1, 3, 2])).fixture, 'הקודם').disabled).toBe(false);
  });

  it('each button emits its own event', () => {
    const { fixture, events } = create(combination('2', [1, 3, 2]));

    buttonByText(fixture, 'הקודם').click();
    buttonByText(fixture, 'הבא').click();
    buttonByText(fixture, 'כל הקומבינציות').click();

    expect(events).toEqual(['previous', 'next', 'all']);
  });

  it('all buttons are disabled while a request is in flight', () => {
    const { fixture } = create(combination('2', [1, 3, 2]), true);

    for (const label of ['הקודם', 'הבא', 'כל הקומבינציות']) {
      expect(buttonByText(fixture, label).disabled).toBe(true);
    }
  });

  it('after "no more combinations": "next" and "all" are disabled, "previous" still works', () => {
    const fixture = TestBed.createComponent(NextViewComponent);
    fixture.componentRef.setInput('current', combination('6', [3, 2, 1]));
    fixture.componentRef.setInput('atEnd', true);
    fixture.detectChanges();

    expect(buttonByText(fixture, 'הבא').disabled).toBe(true);
    expect(buttonByText(fixture, 'כל הקומבינציות').disabled).toBe(true);
    expect(buttonByText(fixture, 'הקודם').disabled).toBe(false);
  });
});
