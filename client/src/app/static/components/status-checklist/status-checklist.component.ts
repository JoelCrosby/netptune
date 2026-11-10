import { Component, input } from '@angular/core';
import {
  LucideCheck,
  LucideCircleDashed,
  LucideLoaderCircle,
  LucideTriangleAlert,
  LucideX,
} from '@lucide/angular';

export type StatusChecklistStatus =
  'passed' | 'warning' | 'failed' | 'skipped' | 'pending';

export interface StatusChecklistItem {
  readonly key: string;
  readonly status: StatusChecklistStatus;
  readonly title: string;
  readonly detail: string;
}

const rowClasses: Record<StatusChecklistStatus, string> = {
  passed: '',
  warning: 'bg-amber-500/8',
  failed: 'bg-red-500/8',
  skipped: 'text-foreground/45',
  pending: '',
};

const iconClasses: Record<StatusChecklistStatus, string> = {
  passed: 'text-emerald-600 dark:text-emerald-400',
  warning: 'text-amber-600 dark:text-amber-300',
  failed: 'text-red-600 dark:text-red-400',
  skipped: 'text-foreground/35',
  pending: 'text-primary',
};

const statusLabels: Record<StatusChecklistStatus, string> = {
  passed: $localize`:Screen reader label for a check that passed:Passed`,
  warning: $localize`:Screen reader label for a check that passed with a warning:Warning`,
  failed: $localize`:Screen reader label for a check that failed:Failed`,
  skipped: $localize`:Screen reader label for a check that was not run:Skipped`,
  pending: $localize`:Screen reader label for a check that is still running:Checking`,
};

@Component({
  selector: 'app-status-checklist',
  imports: [
    LucideCheck,
    LucideCircleDashed,
    LucideLoaderCircle,
    LucideTriangleAlert,
    LucideX,
  ],
  host: { class: 'block' },
  template: `
    @if (heading(); as heading) {
      <p
        class="text-muted m-0 mb-1.5 text-[11px] font-semibold tracking-[0.07em] uppercase">
        {{ heading }}
      </p>
    }
    <ul
      class="border-border divide-border m-0 list-none divide-y overflow-hidden rounded-md border p-0">
      @for (item of items(); track item.key) {
        <li
          class="grid grid-cols-[1.25rem_1fr] gap-2.5 px-3 py-2.5"
          [class]="rowClass(item)">
          <span
            class="flex h-5 w-5 items-center justify-center"
            [class]="iconClass(item)">
            @switch (item.status) {
              @case ('passed') {
                <svg lucideCheck class="h-4.5 w-4.5" aria-hidden="true"></svg>
              }
              @case ('warning') {
                <svg
                  lucideTriangleAlert
                  class="h-4.5 w-4.5"
                  aria-hidden="true"></svg>
              }
              @case ('failed') {
                <svg lucideX class="h-4.5 w-4.5" aria-hidden="true"></svg>
              }
              @case ('pending') {
                <svg
                  lucideLoaderCircle
                  class="h-4.5 w-4.5 motion-safe:animate-spin"
                  aria-hidden="true"></svg>
              }
              @default {
                <svg
                  lucideCircleDashed
                  class="h-4.5 w-4.5"
                  aria-hidden="true"></svg>
              }
            }
          </span>
          <div class="min-w-0">
            <p class="m-0 font-medium">
              <span class="sr-only">{{ statusLabel(item) }}:</span>
              {{ item.title }}
            </p>
            @if (item.detail) {
              <p class="text-muted m-0 mt-px text-[13px]">{{ item.detail }}</p>
            }
          </div>
        </li>
      }
    </ul>
  `,
})
export class StatusChecklistComponent {
  readonly items = input.required<readonly StatusChecklistItem[]>();
  readonly heading = input<string | null>(null);

  protected rowClass(item: StatusChecklistItem): string {
    return rowClasses[item.status];
  }

  protected iconClass(item: StatusChecklistItem): string {
    return iconClasses[item.status];
  }

  protected statusLabel(item: StatusChecklistItem): string {
    return statusLabels[item.status];
  }
}
