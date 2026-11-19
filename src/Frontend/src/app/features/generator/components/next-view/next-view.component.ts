import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CombinationDto } from '../../data-access/generator-api.models';
import { GroupDigitsPipe } from '../../presentation/group-digits.pipe';

/** מסך "הבא": הקומבינציה הנוכחית, וכפתורי "הבא" ו"כל הקומבינציות". */
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

  readonly nextRequested = output<void>();
  readonly allRequested = output<void>();
}
