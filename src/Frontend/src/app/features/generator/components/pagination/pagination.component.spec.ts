import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';
import { page } from '../../../../../testing/fixtures';
import { buttonByText, submitForm, text, typeInto } from '../../../../../testing/dom';
import { PageDto, PageNavigation } from '../../data-access/generator-api.models';
import { PaginationComponent } from './pagination.component';

describe('PaginationComponent', () => {
  // מאפסים את מודול הבדיקה אחרי כל בדיקה, כך שכל בדיקה מתחילה נקייה
  afterEach(() => TestBed.resetTestingModule());

  // דף 6 מתוך 12: 1 … 4 5 [6] 7 8 … 12
  const middle = page({
    pageNumber: '6',
    totalPages: '12',
    links: [
      { type: 'page', number: '1', isCurrent: false },
      { type: 'ellipsis', number: null, isCurrent: false },
      { type: 'page', number: '4', isCurrent: false },
      { type: 'page', number: '5', isCurrent: false },
      { type: 'page', number: '6', isCurrent: true },
      { type: 'page', number: '7', isCurrent: false },
      { type: 'page', number: '8', isCurrent: false },
      { type: 'ellipsis', number: null, isCurrent: false },
      { type: 'page', number: '12', isCurrent: false },
    ],
  });

  function create(value: PageDto) {
    const fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('page', value);
    const emitted: PageNavigation[] = [];
    fixture.componentInstance.navigateRequested.subscribe(n => emitted.push(n));
    fixture.detectChanges();
    return { fixture, emitted };
  }

  it('shows "page X of Y" with grouped digits', () => {
    const { fixture } = create(page({ pageNumber: '12164510040883200', totalPages: '12164510040883200' }));

    expect(text(fixture)).toContain('דף 12,164,510,040,883,200 מתוך 12,164,510,040,883,200');
  });

  it('renders the links in the order received; "…" is not a button', () => {
    const { fixture } = create(middle);

    const numbers = Array.from(fixture.nativeElement.querySelectorAll('.page-number')).map(b => (b as HTMLElement).textContent?.trim());
    expect(numbers).toEqual(['1', '4', '5', '6', '7', '8', '12']);
    expect(fixture.nativeElement.querySelectorAll('.ellipsis').length).toBe(2);
    expect(fixture.nativeElement.querySelector('.ellipsis').tagName).toBe('SPAN');
  });

  it('the current page is marked and cannot be clicked', () => {
    const { fixture } = create(middle);

    const current = fixture.nativeElement.querySelector('[aria-current="page"]') as HTMLButtonElement;
    expect(current.textContent?.trim()).toBe('6');
    expect(current.disabled).toBe(true);
  });

  it('first page: "previous" is disabled; last page: "next" is disabled', () => {
    const first = create(page({ pageNumber: '1', totalPages: '3' })).fixture;
    expect(buttonByText(first, 'דף קודם').disabled).toBe(true);
    expect(buttonByText(first, 'דף הבא').disabled).toBe(false);

    const last = create(page({ pageNumber: '3', totalPages: '3' })).fixture;
    expect(buttonByText(last, 'דף הבא').disabled).toBe(true);
    expect(buttonByText(last, 'דף קודם').disabled).toBe(false);
  });

  it('only the numbers lead to the first and last page: no separate "first" / "last" buttons', () => {
    const { fixture, emitted } = create(middle);

    const labels = Array.from(fixture.nativeElement.querySelectorAll('button')).map(b => (b as HTMLElement).textContent?.trim());
    expect(labels).not.toContain('דף ראשון');
    expect(labels).not.toContain('דף אחרון');

    buttonByText(fixture, '1').click();
    buttonByText(fixture, '12').click();
    expect(emitted).toEqual([
      { action: 'Specific', pageNumber: '1' },
      { action: 'Specific', pageNumber: '12' },
    ]);
  });

  it('"previous" and "next" emit their action, without a page number', () => {
    const { fixture, emitted } = create(middle);

    buttonByText(fixture, 'דף קודם').click();
    buttonByText(fixture, 'דף הבא').click();

    expect(emitted).toEqual([{ action: 'Previous' }, { action: 'Next' }]);
  });

  it('a page link emits Specific with the exact number string, even beyond 2^53', () => {
    const { fixture, emitted } = create(
      page({
        pageNumber: '1',
        totalPages: '2432902008176639999',
        links: [
          { type: 'page', number: '1', isCurrent: true },
          { type: 'ellipsis', number: null, isCurrent: false },
          { type: 'page', number: '2432902008176639999', isCurrent: false },
        ],
      }),
    );

    buttonByText(fixture, '2,432,902,008,176,639,999').click();

    expect(emitted).toEqual([{ action: 'Specific', pageNumber: '2432902008176639999' }]);
  });

  it('jump: a valid page number is sent as text', () => {
    const { fixture, emitted } = create(middle);

    typeInto(fixture, '#jump-page', '9');
    submitForm(fixture);

    expect(emitted).toEqual([{ action: 'Specific', pageNumber: '9' }]);
  });

  it.each(['', '0', '01', '-1', '1e3', ' 5', 'abc'])('jump: rejects "%s" locally and sends nothing', value => {
    const { fixture, emitted } = create(middle);

    typeInto(fixture, '#jump-page', value);
    submitForm(fixture);

    expect(emitted).toEqual([]);
    expect(text(fixture)).toContain('יש להזין מספר דף חיובי.');
  });

  it('jump: the field and its error clear when a new page arrives', () => {
    const { fixture } = create(middle);
    typeInto(fixture, '#jump-page', '01');
    submitForm(fixture);
    expect(text(fixture)).toContain('יש להזין מספר דף חיובי.');

    fixture.componentRef.setInput('page', page({ pageNumber: '7', totalPages: '12' }));
    fixture.detectChanges();
    fixture.detectChanges(); // פעם נוספת: ה-effect מנקה את הטופס, והתבנית מתעדכנת אחריו

    expect(fixture.nativeElement.querySelector('#jump-page').value).toBe('');
    expect(text(fixture)).not.toContain('יש להזין מספר דף חיובי.');
  });
});
