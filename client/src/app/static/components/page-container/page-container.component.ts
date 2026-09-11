import {
  booleanAttribute,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { LayoutService } from '@core/services/layout.service';
import { PageWidthService } from '@core/services/page-width.service';
import { map, of, switchMap, timer } from 'rxjs';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';

const progressRevealDelayMs = 200;

export type PageContainerLayout = 'default' | 'list';

@Component({
  selector: 'app-page-container',
  imports: [ProgressBarComponent],
  host: { '[class]': 'hostClass()' },
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

    @if (stickyFooter()) {
      <div
        class="border-border bg-card sticky bottom-0 z-10 border-t shadow-[0_-8px_24px_var(--card-shadow)]">
        <div [class]="footerRowClass()">
          <ng-content select="[pageFooter]" />
        </div>
      </div>
    }
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

  // Pins a bar to the bottom of the page for content projected into [pageFooter]. The bar runs
  // edge to edge while the row inside it keeps the centred cap, the way the list layout's header
  // band does, so a page's actions reach the sidebar rather than stopping at the content column.
  readonly stickyFooter = input(false, { transform: booleanAttribute });

  // Default-layout pages keep the centred cap unless they opt in, so forms and
  // detail views stay readable while width-filling pages follow the preference.
  // List pages always follow it.
  readonly followsWidthPreference = input(false, {
    transform: booleanAttribute,
  });

  private readonly pageWidth = inject(PageWidthService);
  private readonly shellLayout = inject(LayoutService);

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

  constructor() {
    const destroyRef = inject(DestroyRef);

    effect(() => this.shellLayout.setPageOwnsScroll(this.isList()));

    destroyRef.onDestroy(() => this.shellLayout.setPageOwnsScroll(false));
  }

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

  // The footer sits outside the content column so it can run edge to edge, which only leaves it at
  // the bottom of the page if the host owns the height and the column above takes the slack. A list
  // page is already exactly one screen tall, so the host takes that height over from the root.
  protected readonly hostClass = computed(() => {
    if (!this.stickyFooter()) return '';

    return this.isList()
      ? 'flex h-[calc(100dvh-60px)] flex-col'
      : 'flex min-h-full flex-col';
  });

  protected readonly rootClass = computed(() => {
    if (this.isList()) {
      const height = this.stickyFooter()
        ? 'min-h-0 flex-1'
        : 'h-[calc(100dvh-60px)]';

      return `relative flex flex-col ${height}`;
    }

    const classes = ['flex flex-col'];

    if (this.stickyFooter()) classes.push('min-h-0 flex-1');

    if (this.centerPage()) {
      classes.push('mx-auto w-full');

      if (this.capWidth()) classes.push('max-w-[1360px]');
    }

    if (this.fullHeight() && !this.marginBottom()) classes.push('h-full');
    if (this.marginBottom()) classes.push('pb-[20vh]');

    return classes.join(' ');
  });

  protected readonly footerRowClass = computed(() => {
    const classes = ['mx-auto w-full'];

    if (this.centerPage() && this.capWidth()) classes.push('max-w-[1360px]');
    if (this.horizontalPadding()) classes.push('px-8 max-md:px-3');

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

    if (this.horizontalPadding()) classes.push('px-8 max-md:px-3');
    if (this.verticalPadding()) classes.push('py-16');
    if (this.fullHeight()) classes.push('h-[calc(100dvh-76px)]');

    return classes.join(' ');
  });
}
