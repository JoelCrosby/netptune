import { Component, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { map, of, switchMap, timer } from 'rxjs';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';

const progressRevealDelayMs = 200;

@Component({
  selector: 'app-page-container',
  imports: [ProgressBarComponent],
  template: `<div
    class="flex flex-col"
    [class.mx-auto]="centerPage()"
    [class.w-full]="centerPage()"
    [class.max-w-[1360px]]="centerPage()"
    [attr.aria-busy]="showProgress()">
    <div
      class="h-[0.8rem] shrink-0"
      [class.invisible]="!progressVisible()"
      [attr.aria-hidden]="progressVisible() ? null : 'true'">
      <app-progress-bar mode="indeterminate" />
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
  readonly centerPage = input<boolean | null>(true);

  readonly progressVisible = toSignal(
    toObservable(this.showProgress).pipe(
      switchMap((showProgress) =>
        showProgress
          ? timer(progressRevealDelayMs).pipe(map(() => true))
          : of(false)
      )
    ),
    { initialValue: false }
  );
}
