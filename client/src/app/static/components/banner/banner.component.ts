import { Component, inject } from '@angular/core';
import { LucideInfo, LucideX } from '@lucide/angular';
import { BannerState } from './banner.models';
import { BannerService } from './banner.service';

@Component({
  selector: 'app-banner',
  imports: [LucideInfo, LucideX],
  template: `
    @if (service.banner(); as banner) {
      <div
        class="mx-4 my-4 rounded bg-primary h-16 text-background fixed bottom-0 left-0 z-9999 flex w-[calc(100vw-32px)] items-center justify-center gap-3 px-4 py-2.5 text-sm leading-tight font-medium shadow-lg"
        role="alert"
        aria-live="assertive">
        <svg lucideInfo class="h-[1.1rem] w-[1.1rem] shrink-0 leading-none"></svg>

        <span>{{ banner.message }}</span>

        @if (banner.action) {
          <button
            class="bg-background text-primary shrink-0 rounded-sm px-3 py-1 text-xs font-semibold tracking-wide uppercase transition-opacity hover:opacity-90"
            (click)="onAction(banner)">
            {{ banner.action }}
          </button>
        }

        @if (banner.dismissible) {
          <button
            class="absolute right-4 shrink-0 leading-none opacity-70 transition-opacity hover:opacity-100"
            aria-label="Dismiss"
            (click)="service.dismiss()">
            <svg lucideX class="h-4 w-4 leading-none"></svg>
          </button>
        }
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: contents;
      }
    `,
  ],
})
export class BannerComponent {
  readonly service = inject(BannerService);

  onAction(banner: BannerState): void {
    banner.onAction?.();
    this.service.dismiss();
  }
}
