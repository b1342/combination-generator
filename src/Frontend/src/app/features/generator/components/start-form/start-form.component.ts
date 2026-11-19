import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

/** מסך ההתחלה: שדה N וכפתור "התחל". בודק את הקלט לפני ששולחים. */
@Component({
  selector: 'app-start-form',
  imports: [ReactiveFormsModule],
  templateUrl: './start-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StartFormComponent {
  /** מושבת בזמן שבקשה באוויר. */
  readonly disabled = input(false);

  /** נשלח עם N תקין כשלוחצים "התחל". */
  readonly startRequested = output<number>();

  readonly form = new FormGroup({
    n: new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(1),
      Validators.max(20),
      Validators.pattern(/^\d+$/), // מספר שלם בלבד
    ]),
  });

  /** מציגים שגיאה רק אחרי שהמשתמשת נגעה בשדה. */
  showError(): boolean {
    const n = this.form.controls.n;
    return n.invalid && (n.touched || n.dirty);
  }

  submit(): void {
    const n = this.form.controls.n;
    if (n.invalid || n.value === null) {
      n.markAsTouched();
      return;
    }

    this.startRequested.emit(n.value);
  }
}
