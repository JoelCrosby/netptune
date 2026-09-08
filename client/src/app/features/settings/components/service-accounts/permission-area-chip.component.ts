import { Component, computed, input, output } from '@angular/core';
import { cn } from '@static/components/button/button.variants';
import {
  PermissionAreaLevel,
  PermissionAreaSummary,
} from './service-account-permissions';

const dotClasses: Record<PermissionAreaLevel, string> = {
  full: 'bg-change-added',
  partial: 'bg-change-modified',
  none: 'bg-foreground/25',
};

const countClasses: Record<PermissionAreaLevel, string> = {
  full: 'text-muted',
  partial: 'text-change-modified',
  none: 'text-foreground/35',
};

@Component({
  selector: 'app-permission-area-chip',
  host: { class: 'contents' },
  template: `
    <button
      type="button"
      [class]="buttonClass()"
      [attr.aria-expanded]="expanded()"
      (click)="toggled.emit()">
      <span class="size-1.75 shrink-0 rounded-xs" [class]="dotClass()"></span>
      <span class="min-w-0 flex-1 truncate text-[13px]">{{
        area().label
      }}</span>
      <span class="shrink-0 font-mono text-[11px]" [class]="countClass()">
        {{ area().granted }}/{{ area().total }}
      </span>
    </button>
  `,
})
export class PermissionAreaChipComponent {
  readonly area = input.required<PermissionAreaSummary>();
  readonly expanded = input(false);

  readonly toggled = output();

  protected readonly dotClass = computed(() => dotClasses[this.area().level]);

  protected readonly countClass = computed(
    () => countClasses[this.area().level]
  );

  protected readonly buttonClass = computed(() => {
    return cn(
      'border-border hover:border-primary/50 flex cursor-pointer items-center gap-2 rounded-md border px-2.5 py-1.75 text-left transition-colors',
      this.surfaceClass()
    );
  });

  private readonly surfaceClass = computed(() => {
    if (this.expanded()) return 'border-primary bg-primary/10';

    const isEmptyArea = this.area().level === 'none';

    return isEmptyArea ? 'bg-card-header opacity-55' : 'bg-card-header';
  });
}
