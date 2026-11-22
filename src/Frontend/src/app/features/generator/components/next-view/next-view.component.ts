import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CombinationDto } from '../../data-access/generator-api.models';
import { GroupDigitsPipe } from '../../presentation/group-digits.pipe';

/** מסך "הבא": הקומבינציה הנוכחית, וכפתורי "הקודם", "הבא" ו"כל הקומבינציות". */
@Component({
  selector: 'app-next-view',
  imports: [GroupDigitsPipe],
  templateUrl: './next-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NextViewComponent {
  /** הקומבינציה האחרונה שהוצגה, או null לפני הלחיצה הראשונה. */
  readonly current = input<CombinationDto | null>(null);
  readonly disabled = input(false);

  /** true אחרי שהשרת ענה "אין יותר קומבינציות": אין טעם ב"הבא" או ב"כל הקומבינציות". "הקודם" עדיין עובד. */
  readonly atEnd = input(false);

  /** אין "הקודם" לפני הקומבינציה הראשונה. השוואת טקסט בלבד, בלי חשבון. */
  readonly hasPrevious = computed(() => {
    const current = this.current();
    return current !== null && current.number !== '1';
  });

  readonly previousRequested = output<void>();
  readonly nextRequested = output<void>();
  readonly allRequested = output<void>();
}
