import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  output,
} from '@angular/core';
import {
  LucideCircleCheck,
  LucideCircleX,
  LucideDynamicIcon,
  LucideIconInput,
  LucideInfo,
  LucideTriangleAlert,
  LucideX,
} from '@lucide/angular';
import { SnackbarItem, SnackbarType } from './snackbar.models';
import { SnackbarService } from './snackbar.service';

const TYPE_STYLES: Record<SnackbarType, string> = {
  default: 'bg-[var(--card)] text-[var(--foreground)] border-[var(--border)]',
  success: 'bg-[#1a3a2a] text-green-300 border-green-800',
  error: 'bg-[#3a1a1a] text-red-300 border-red-800',
  warn: 'bg-[#3a2e1a] text-yellow-300 border-yellow-800',
  info: 'bg-[#1a2a3a] text-blue-300 border-blue-800',
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
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideDynamicIcon, LucideX],
  template: `
    <div
      class="flex max-w-[480px] min-w-[280px] items-center gap-3 rounded-[var(--radius-sm)] border px-4 py-3 text-sm leading-tight font-medium shadow-lg"
      [class]="typeStyles()"
      role="status"
      aria-live="polite">
      @if (icon()) {
        <svg
          [lucideIcon]="icon()!"
          class="h-[1.1rem] w-[1.1rem] shrink-0 leading-none"></svg>
      }

      <span class="flex-1">{{ item().message }}</span>

      @if (item().action) {
        <button
          class="shrink-0 text-xs font-semibold tracking-wide uppercase opacity-80 transition-opacity hover:opacity-100"
          (click)="actionClicked.emit(item())">
          {{ item().action }}
        </button>
      }

      <button
        class="shrink-0 leading-none opacity-50 transition-opacity hover:opacity-100"
        aria-label="Dismiss"
        (click)="snackbarService.dismiss(item().id)">
        <svg lucideX class="h-[1rem] w-[1rem] leading-none"></svg>
      </button>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        animation: snackbar-enter 0.2s ease;
      }

      @keyframes snackbar-enter {
        from {
          opacity: 0;
          transform: translateY(12px) scale(0.96);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }
    `,
  ],
})
export class SnackbarComponent {
  readonly item = input.required<SnackbarItem>();
  readonly actionClicked = output<SnackbarItem>();

  readonly snackbarService = inject(SnackbarService);

  typeStyles() {
    return TYPE_STYLES[this.item().type];
  }

  icon() {
    return TYPE_ICONS[this.item().type];
  }
}
