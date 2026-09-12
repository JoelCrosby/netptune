import { Component } from '@angular/core';
import { LucideLock } from '@lucide/angular';

@Component({
  selector: 'app-turnstile-notice',
  imports: [LucideLock],
  host: {
    class: 'text-foreground/45 flex shrink-0 items-center gap-1.5 text-xs',
  },
  template: `
    <svg lucideLock size="13" aria-hidden="true"></svg>
    <ng-container
      i18n="
        Notes that the sign-in form is guarded by the Cloudflare Turnstile bot
        check. Turnstile is a product name and must not be translated
      ">
      Protected by Turnstile
    </ng-container>
  `,
})
export class TurnstileNoticeComponent {}
