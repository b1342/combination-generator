import { ChangeDetectionStrategy, Component } from '@angular/core';
import { GeneratorPageComponent } from './features/generator/generator-page.component';

/** הרכיב הראשי: רק "מסגרת" שמציגה את מסך המחולל. */
@Component({
  selector: 'app-root',
  imports: [GeneratorPageComponent],
  template: '<app-generator-page />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {}
