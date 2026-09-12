import { Component, computed, input } from '@angular/core';
import { AuthSidebarComponent } from '../auth-sidebar/auth-sidebar.component';

export type AuthPageLayout = 'wide' | 'narrow';
export type AuthPageAlign = 'center' | 'start';

const gridClasses: Record<AuthPageLayout, string> = {
  wide: 'lg:grid-cols-[520px_1fr]',
  narrow: 'lg:grid-cols-[420px_1fr]',
};

const surfaceClasses: Record<AuthPageAlign, string> = {
  center: 'flex items-center justify-center px-4 py-10 sm:px-10',
  start: 'px-4 pt-12 pb-10 sm:px-12 sm:pt-14',
};

@Component({
  selector: 'app-auth-page-container',
  imports: [AuthSidebarComponent],
  host: { '[class]': 'hostClass()' },
  template: `
    <app-auth-sidebar class="hidden lg:flex" />

    <div
      class="auth-page-surface min-w-0 overflow-y-auto"
      [class]="surfaceClass()">
      <ng-content />
    </div>
  `,
  styles: [
    `
      .auth-page-surface {
        background-color: var(--background);
        background-image: radial-gradient(
          color-mix(in oklab, var(--foreground) 17%, transparent) 1px,
          transparent 1px
        );
        background-size: 20px 20px;
      }
    `,
  ],
})
export class AuthPageContainerComponent {
  readonly layout = input<AuthPageLayout>('wide');
  readonly align = input<AuthPageAlign>('center');

  protected readonly hostClass = computed(() => {
    return `grid h-dvh grid-cols-1 ${gridClasses[this.layout()]}`;
  });

  protected readonly surfaceClass = computed(
    () => surfaceClasses[this.align()]
  );
}
