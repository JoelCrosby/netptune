import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';

@Component({
  selector: 'app-page-container',
  templateUrl: './page-container.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ProgressBarComponent],
})
export class PageContainerComponent {
  readonly verticalPadding = input<boolean | null>(false);
  readonly showProgress = input<boolean | null>(false);
  readonly marginBottom = input<boolean | null>(false);
  readonly fullHeight = input<boolean | null>(true);
  readonly centerPage = input<boolean | null>(false);
}
