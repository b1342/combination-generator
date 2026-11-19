import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { AllViewComponent } from './components/all-view/all-view.component';
import { NextViewComponent } from './components/next-view/next-view.component';
import { StartFormComponent } from './components/start-form/start-form.component';
import { GeneratorFacade } from './generator.facade';
import { GroupDigitsPipe } from './presentation/group-digits.pipe';

/**
 * המסך כולו. מחליט איזה חלק להציג (התחלה / "הבא" / "כל הקומבינציות"),
 * ומחבר את הלחיצות ברכיבים לפונקציות של ה-Facade.
 */
@Component({
  selector: 'app-generator-page',
  imports: [StartFormComponent, NextViewComponent, AllViewComponent, GroupDigitsPipe],
  templateUrl: './generator-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // Facade משלו לכל מסך: המצב שייך למסך הזה בלבד
  providers: [GeneratorFacade],
})
export class GeneratorPageComponent implements OnInit {
  protected readonly facade = inject(GeneratorFacade);

  ngOnInit(): void {
    this.facade.initialize();
  }
}
