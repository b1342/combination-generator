import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { PageDto, PageNavigation } from '../../data-access/generator-api.models';
import { PAGE_SIZE_OPTIONS } from '../../generator.config';
import { GroupDigitsPipe } from '../../presentation/group-digits.pipe';
import { PaginationComponent } from '../pagination/pagination.component';

/** מסך "כל הקומבינציות": בחירת גודל עמוד, טבלת העמוד, הדפדוף, ו"חזור". */
@Component({
  selector: 'app-all-view',
  imports: [GroupDigitsPipe, PaginationComponent],
  templateUrl: './all-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AllViewComponent {
  readonly page = input.required<PageDto>();
  readonly disabled = input(false);

  readonly navigateRequested = output<PageNavigation>();
  readonly pageSizeChanged = output<number>();
  readonly backRequested = output<void>();

  /** האפשרויות ברשימה, כולל הגודל הנוכחי אם הוא לא אחד מהקבועים. */
  readonly pageSizes = computed(() =>
    [...new Set([...PAGE_SIZE_OPTIONS, this.page().pageSize])].sort((a, b) => a - b));

  onPageSizeChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const value = Number(select.value);

    // הבורר חוזר לגודל הנוכחי, ויתעדכן רק כשהשרת יאשר את הגודל החדש.
    // כך, אם הבקשה נכשלת, לא נשאר על המסך גודל שלא באמת מוצג.
    select.value = String(this.page().pageSize);
    this.pageSizeChanged.emit(value);
  }
}
