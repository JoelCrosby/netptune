import { Component, ChangeDetectionStrategy, input } from '@angular/core';

import { MatProgressBar } from '@angular/material/progress-bar';

@Component({
  selector: 'app-page-container',
  templateUrl: './page-container.component.html',
  styleUrls: ['./page-container.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatProgressBar],
})
export class PageContainerComponent {
  readonly verticalPadding = input<boolean | null>(false);
  readonly showProgress = input<boolean | null>(false);
  readonly marginBottom = input<boolean | null>(false);
  readonly fullHeight = input<boolean | null>(true);
  readonly centerPage = input<boolean | null>(false);
}
