import { Component, computed, input, model } from '@angular/core';
import { cn } from '../button/button.variants';

export interface TabItem {
  label: string;
  value: string | number | null;
  badge?: number;
  count?: number | null;
}

export type TabGroupVariant = 'default' | 'island';

@Component({
  selector: 'app-tab-group',
  host: {
    role: 'tablist',
    '[class]': 'hostClass()',
  },
  template: `
    @for (tab of tabs(); track tab.value) {
      <button
        role="tab"
        type="button"
        [attr.aria-selected]="value() === tab.value"
        [class]="tabClass(tab.value)"
        (click)="value.set(tab.value)">
        {{ tab.label }}

        @if (tab.badge !== undefined) {
          <span [class]="badgeClass(tab.value)">{{ tab.badge }}</span>
        }

        @if (tab.count !== null && tab.count !== undefined) {
          <span class="text-muted ml-1.5 font-medium">{{ tab.count }}</span>
        }
      </button>
    }

    <ng-content />
  `,
})
export class TabGroupComponent {
  readonly tabs = input.required<TabItem[]>();
  readonly value = model<string | number | null>(null);
  readonly variant = input<TabGroupVariant>('default');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'flex items-center overflow-x-auto',
      this.variant() === 'default'
        ? 'border-border border-b'
        : 'border-border bg-card w-fit max-w-full shrink-0 gap-0.5 rounded-[10px] border p-1',
      this.class()
    );
  });

  protected tabClass(tabValue: string | number | null): string {
    const isActive = this.value() === tabValue;

    if (this.variant() === 'default') {
      return cn(
        'inline-flex shrink-0 cursor-pointer items-center gap-2 border-b-2 px-3 pt-1 pb-2.5 text-sm font-medium transition-colors focus-visible:outline-none',
        isActive
          ? 'border-primary text-foreground'
          : 'border-transparent text-muted hover:text-foreground hover:border-border'
      );
    }

    return cn(
      'focus-visible:ring-primary inline-flex h-7 shrink-0 cursor-pointer items-center rounded-[7px] border px-2.5 text-[13px] transition-colors outline-none focus-visible:ring-2',
      isActive
        ? 'border-border bg-hover text-foreground font-semibold'
        : 'border-transparent text-muted hover:bg-hover/60 hover:text-foreground font-medium'
    );
  }

  protected badgeClass(tabValue: string | number | null): string {
    return cn(
      'inline-flex h-5 min-w-5 items-center justify-center rounded-full px-1 text-xs font-medium',
      this.value() === tabValue
        ? 'bg-primary text-white'
        : 'bg-foreground/10 text-foreground'
    );
  }
}
