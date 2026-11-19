import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';
import { combination, page } from '../../../../../testing/fixtures';
import { buttonByText, text } from '../../../../../testing/dom';
import { PageDto } from '../../data-access/generator-api.models';
import { AllViewComponent } from './all-view.component';

describe('AllViewComponent', () => {
  // מאפסים את מודול הבדיקה אחרי כל בדיקה, כך שכל בדיקה מתחילה נקייה
  afterEach(() => TestBed.resetTestingModule());

  function create(value: PageDto = page({ pageSize: 20 })) {
    const fixture = TestBed.createComponent(AllViewComponent);
    fixture.componentRef.setInput('page', value);

    const sizes: number[] = [];
    let backs = 0;
    fixture.componentInstance.pageSizeChanged.subscribe(size => sizes.push(size));
    fixture.componentInstance.backRequested.subscribe(() => backs++);

    fixture.detectChanges();
    return { fixture, sizes, back: () => backs };
  }

  it('renders the rows exactly as received, with grouped serial numbers', () => {
    const { fixture } = create(
      page({ rows: [combination('2432902008176639801', [20, 19, 18]), combination('2432902008176639802', [20, 18, 19])] }),
    );

    // כל שורה כרשימת תאים: Angular מסיר רווחים בין תגיות, אז קוראים כל תא בנפרד
    const rows = Array.from(fixture.nativeElement.querySelectorAll('tbody tr')).map(row =>
      Array.from((row as HTMLElement).querySelectorAll('td'))
        .map(cell => cell.textContent?.trim())
        .join(' '),
    );
    expect(rows).toEqual(['2,432,902,008,176,639,801 [20, 19, 18]', '2,432,902,008,176,639,802 [20, 18, 19]']);
  });

  it('offers 5, 10, 20, 50, 100 and selects the current size', () => {
    const { fixture } = create(page({ pageSize: 20 }));

    const select = fixture.nativeElement.querySelector('#page-size') as HTMLSelectElement;
    expect(Array.from(select.options).map(o => o.value)).toEqual(['5', '10', '20', '50', '100']);
    expect(select.value).toBe('20');
  });

  it('adds an unusual current size to the list (e.g. 2, from a refresh)', () => {
    const { fixture } = create(page({ pageSize: 2 }));

    const select = fixture.nativeElement.querySelector('#page-size') as HTMLSelectElement;
    expect(Array.from(select.options).map(o => o.value)).toEqual(['2', '5', '10', '20', '50', '100']);
    expect(select.value).toBe('2');
  });

  it('changing the size emits it, and the select waits for the server', () => {
    const { fixture, sizes } = create(page({ pageSize: 20 }));
    const select = fixture.nativeElement.querySelector('#page-size') as HTMLSelectElement;

    select.value = '50';
    select.dispatchEvent(new Event('change'));

    expect(sizes).toEqual([50]);
    expect(select.value).toBe('20'); // עד שהשרת יאשר
  });

  it('"back" emits', () => {
    const { fixture, back } = create();

    buttonByText(fixture, 'חזור').click();

    expect(back()).toBe(1);
  });

  it('contains the pagination', () => {
    const { fixture } = create(page({ pageNumber: '1', totalPages: '2' }));

    expect(text(fixture)).toContain('דף 1 מתוך 2');
  });
});
