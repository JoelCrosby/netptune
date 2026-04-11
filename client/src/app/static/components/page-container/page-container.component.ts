import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';

@Component({
  selector: 'app-page-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ProgressBarComponent],
  template: `<div
    class="flex h-screen flex-col"
    [class.mx-auto]="centerPage()"
    [class.w-full]="centerPage()"
    [class.max-w-[1200px]]="centerPage()">
    <div class="h-[0.8rem] shrink-0">
      @if (showProgress()) {
        <app-progress-bar mode="indeterminate" />
      }
    </div>
    <div
      class="flex flex-1 flex-col px-8 max-[600px]:px-3"
      [class.py-16]="verticalPadding()"
      [class.pb-[20vh]]="marginBottom()"
      [class.h-[calc(100vh-56px)]]="fullHeight()">
      <ng-content />
    </div>
  </div> `,
})
export class PageContainerComponent {
  readonly verticalPadding = input<boolean | null>(false);
  readonly showProgress = input<boolean | null>(false);
  readonly marginBottom = input<boolean | null>(false);
  readonly fullHeight = input<boolean | null>(true);
  readonly centerPage = input<boolean | null>(false);
}
