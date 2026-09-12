import { Component } from '@angular/core';
import { AuthSidebarComponent } from '../auth-sidebar/auth-sidebar.component';

@Component({
  selector: 'app-auth-page-container',
  imports: [AuthSidebarComponent],
  host: {
    class: 'grid h-dvh grid-cols-1 lg:grid-cols-[520px_1fr]',
  },
  template: `
    <app-auth-sidebar class="hidden lg:flex" />

    <div
      class="auth-page-surface flex min-w-0 items-center justify-center overflow-y-auto px-4 py-10 sm:px-10">
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
export class AuthPageContainerComponent {}
