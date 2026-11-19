import { ChangeDetectionStrategy, Component, computed, effect, input, output, untracked } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageAction, PageDto, PageLinkDto, PageNavigation } from '../../data-access/generator-api.models';
import { GroupDigitsPipe } from '../../presentation/group-digits.pipe';

/**
 * כפתורי הדפדוף: ראשון, קודם, מספרי הדפים, הבא, אחרון, וקפיצה לדף.
 * לא מחשב איזה דף לבקש: רק אומר "לחצו על הבא", והשרת מחליט.
 */
@Component({
  selector: 'app-pagination',
  imports: [ReactiveFormsModule, GroupDigitsPipe],
  templateUrl: './pagination.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginationComponent {
  readonly page = input.required<PageDto>();
  readonly disabled = input(false);

  readonly navigateRequested = output<PageNavigation>();

  // השוואת טקסט בלבד, בלי חשבון: "האם הדף הנוכחי הוא '1'?" / "האם הוא שווה למספר הדפים?"
  readonly isFirstPage = computed(() => this.page().pageNumber === '1');
  readonly isLastPage = computed(() => this.page().pageNumber === this.page().totalPages);

  /** שדה הקפיצה לדף. טקסט ולא מספר: מספרי דפים יכולים להיות ענקיים. */
  readonly jumpForm = new FormGroup({
    pageNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[1-9][0-9]{0,18}$/)], // מספר חיובי, בלי אפסים מובילים
    }),
  });

  constructor() {
    // עמוד חדש הגיע מהשרת: מנקים את שדה הקפיצה ואת הודעת השגיאה שלו.
    // אם הבקשה נכשלה, העמוד לא משתנה, והטקסט נשאר לתיקון.
    effect(() => {
      this.page();
      untracked(() => this.jumpForm.reset());
    });
  }

  go(action: PageAction): void {
    this.navigateRequested.emit({ action });
  }

  goToLink(link: PageLinkDto): void {
    if (link.number !== null) {
      this.navigateRequested.emit({ action: 'Specific', pageNumber: link.number });
    }
  }

  showJumpError(): boolean {
    const control = this.jumpForm.controls.pageNumber;
    return control.invalid && control.touched;
  }

  submitJump(): void {
    const control = this.jumpForm.controls.pageNumber;
    if (control.invalid) {
      control.markAsTouched();
      return;
    }

    this.navigateRequested.emit({ action: 'Specific', pageNumber: control.value });
  }
}
