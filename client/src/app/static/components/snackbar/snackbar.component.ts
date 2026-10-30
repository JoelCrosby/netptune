import { Component, inject, input, output } from '@angular/core';
import {
  LucideCircleCheck,
  LucideCircleX,
  LucideDynamicIcon,
  LucideIconInput,
  LucideInfo,
  LucideTriangleAlert,
  LucideX,
} from '@lucide/angular';
import { IconButtonComponent } from '../button/icon-button.component';
import { signalToneSoft, toneClasses } from '../tones';
import { SnackbarItem, SnackbarType } from './snackbar.models';
import { SnackbarService } from './snackbar.service';

const TYPE_TILES: Record<SnackbarType, string> = {
  default: toneClasses.purple,
  success: signalToneSoft.good,
  error: signalToneSoft.bad,
  warn: signalToneSoft.warn,
  info: toneClasses.blue,
};

const TYPE_ICONS: Record<SnackbarType, LucideIconInput | null> = {
  default: null,
  success: LucideCircleCheck,
  error: LucideCircleX,
  warn: LucideTriangleAlert,
  info: LucideInfo,
};

@Component({
  selector: 'app-snackbar',
  imports: [IconButtonComponent, LucideDynamicIcon, LucideX],
  template: `
    <div
      class="border-border bg-dialog-background flex items-center gap-3 rounded-xl border py-2 pr-2 pl-3 shadow-xl md:max-w-md md:min-w-80"
      role="status"
      aria-live="polite">
      @if (icon(); as icon) {
        <span
          class="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg"
          [class]="tileClass()"
          aria-hidden="true">
          <svg [lucideIcon]="icon" class="h-3.5 w-3.5"></svg>
        </span>
      }

      <span class="text-foreground flex-1 py-1 text-[13px] leading-snug">
        {{ item().message }}
      </span>

      @if (item().action) {
        <button
          type="button"
          class="text-primary shrink-0 rounded-md px-1.5 py-1 text-[13px] font-medium hover:underline"
          (click)="actionClicked.emit(item())">
          {{ item().action }}
        </button>
      }

      <button
        app-icon-button
        type="button"
        color="muted"
        size="xs"
        class="shrink-0 rounded-lg"
        i18n-aria-label="
          Accessible label for the button that dismisses a notification toast
        "
        aria-label="Dismiss"
        (click)="snackbarService.dismiss(item().id)">
        <svg lucideX class="h-3.5 w-3.5"></svg>
      </button>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        animation: snackbar-enter 160ms ease-out;
      }

      @keyframes snackbar-enter {
        from {
          opacity: 0;
          transform: translateY(-12px) scale(0.98);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }

      @media (min-width: 48rem) {
        @keyframes snackbar-enter {
          from {
            opacity: 0;
            transform: translateY(12px) scale(0.98);
          }
          to {
            opacity: 1;
            transform: none;
          }
        }
      }

      @media (prefers-reduced-motion: reduce) {
        :host {
          animation: none;
        }
      }
    `,
  ],
})
export class SnackbarComponent {
  readonly item = input.required<SnackbarItem>();
  readonly actionClicked = output<SnackbarItem>();

  readonly snackbarService = inject(SnackbarService);

  tileClass() {
    return TYPE_TILES[this.item().type];
  }

  icon() {
    return TYPE_ICONS[this.item().type];
  }
}
