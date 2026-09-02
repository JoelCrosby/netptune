import {
  booleanAttribute,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { PageWidthService } from '@core/services/page-width.service';
import { map, of, switchMap, timer } from 'rxjs';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';

const progressRevealDelayMs = 200;

export type PageContainerLayout = 'default' | 'list';

@Component({
  selector: 'app-page-container',
  imports: [ProgressBarComponent],
  template: `
    <div [class]="rootClass()" [attr.aria-busy]="showProgress()">
      <div
        [class]="progressClass()"
        [attr.aria-hidden]="progressVisible() ? null : 'true'">
        <app-progress-bar mode="indeterminate" />
      </div>
      <div [class]="contentClass()">
        <ng-content />
      </div>
    </div>
  `,
})
export class PageContainerComponent {
  readonly verticalPadding = input<boolean | null>(false);
  readonly horizontalPadding = input<boolean | null>(true);
  readonly showProgress = input<boolean | null>(false);
  readonly marginBottom = input<boolean | null>(false);
  readonly fullHeight = input<boolean | null>(true);
  readonly centerPage = input<boolean | null>(true);

  readonly layout = input<PageContainerLayout>('default');

  // Default-layout pages keep the centred cap unless they opt in, so forms and
  // detail views stay readable while width-filling pages follow the preference.
  // List pages always follow it.
  readonly followsWidthPreference = input(false, {
    transform: booleanAttribute,
  });

  private readonly pageWidth = inject(PageWidthService);

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

  private readonly isList = computed(() => this.layout() === 'list');

  private readonly capWidth = computed(() => {
    const followsPreference = this.isList() || this.followsWidthPreference();

    return !followsPreference || this.pageWidth.centered();
  });

  // Read by PageHeaderComponent and PageBodyComponent through the element
  // injector, so the band and the body can run edge to edge while what sits
  // inside them keeps the centred max width. Off when the user asked for full
  // width pages.
  readonly constrainListContent = computed(() => {
    return this.isList() && this.centerPage() !== false && this.capWidth();
  });

  protected readonly rootClass = computed(() => {
    if (this.isList()) {
      return 'relative flex h-[calc(100vh-60px)] flex-col';
    }

    const classes = ['flex flex-col'];

    if (this.centerPage()) {
      classes.push('mx-auto w-full');

      if (this.capWidth()) classes.push('max-w-[1360px]');
    }

    if (this.fullHeight() && !this.marginBottom()) classes.push('h-full');
    if (this.marginBottom()) classes.push('pb-[20vh]');

    return classes.join(' ');
  });

  protected readonly progressClass = computed(() => {
    const hidden = this.progressVisible() ? '' : 'invisible';

    if (this.isList()) {
      return `pointer-events-none absolute inset-x-0 top-0 z-20 ${hidden}`;
    }

    return `h-3 shrink-0 ${hidden}`;
  });

  protected readonly contentClass = computed(() => {
    if (this.isList()) {
      return 'flex min-h-0 flex-1 flex-col';
    }

    const classes = ['flex flex-1 flex-col'];

    if (this.horizontalPadding()) classes.push('px-8 max-[600px]:px-3');
    if (this.verticalPadding()) classes.push('py-16');
    if (this.fullHeight()) classes.push('h-[calc(100vh-76px)]');

    return classes.join(' ');
  });
}
