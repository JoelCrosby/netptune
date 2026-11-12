import {
  booleanAttribute,
  Component,
  computed,
  ElementRef,
  input,
  model,
  viewChildren,
} from '@angular/core';
import { cn } from '../button/button.variants';

export interface TabItem {
  label: string;
  value: string | number | null;
  badge?: number;
  count?: number | null;
}

export type TabGroupVariant = 'default' | 'island' | 'subtle';

const groupClasses: Record<TabGroupVariant, string> = {
  default: 'border-border border-b',
  island:
    'border-border bg-card w-fit max-w-full shrink-0 gap-0.5 rounded-[10px] border p-1',
  subtle:
    'border-border w-fit max-w-full shrink-0 gap-0.5 rounded-lg border p-0.5',
};

@Component({
  selector: 'app-tab-group',
  host: {
    role: 'tablist',
    '[class]': 'hostClass()',
  },
  template: `
    @for (tab of tabs(); track tab.value; let index = $index) {
      <button
        #tabButton
        role="tab"
        type="button"
        [attr.aria-selected]="value() === tab.value"
        [attr.tabindex]="tab.value === focusableValue() ? 0 : -1"
        [class]="tabClass(tab.value)"
        (click)="value.set(tab.value)"
        (keydown)="onKeydown($event, index)">
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
  readonly stretch = input(false, { transform: booleanAttribute });
  readonly class = input('');

  private readonly tabButtons =
    viewChildren<ElementRef<HTMLButtonElement>>('tabButton');

  protected readonly focusableValue = computed(() => {
    const tabs = this.tabs();
    const value = this.value();
    const hasSelectedTab = tabs.some((tab) => tab.value === value);

    return hasSelectedTab ? value : tabs[0]?.value;
  });

  protected readonly hostClass = computed(() => {
    return cn(
      'flex items-center overflow-x-auto',
      groupClasses[this.variant()],
      this.stretch() && 'w-full',
      this.class()
    );
  });

  protected tabClass(tabValue: string | number | null): string {
    const isActive = this.value() === tabValue;

    if (this.variant() === 'default') {
      return cn(
        'focus-visible:ring-primary inline-flex shrink-0 cursor-pointer items-center gap-2 rounded-t-md border-b-2 px-3 pt-1 pb-2.5 text-sm font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none focus-visible:ring-inset',
        isActive
          ? 'border-primary text-foreground'
          : 'border-transparent text-muted hover:text-foreground hover:border-border'
      );
    }

    if (this.variant() === 'subtle') {
      return cn(
        'focus-visible:ring-primary inline-flex h-[26px] shrink-0 cursor-pointer items-center rounded-md px-2.5 text-[12.5px] font-medium transition-colors outline-none focus-visible:ring-2',
        this.stretch() && 'flex-1 justify-center',
        isActive
          ? 'bg-foreground/8 text-foreground'
          : 'text-muted hover:text-foreground'
      );
    }

    return cn(
      'focus-visible:ring-primary inline-flex h-7 shrink-0 cursor-pointer items-center rounded-[7px] border px-2.5 text-[13px] transition-colors outline-none focus-visible:ring-2',
      this.stretch() && 'flex-1 justify-center',
      isActive
        ? 'border-border bg-hover text-foreground font-semibold'
        : 'border-transparent text-muted hover:bg-hover/60 hover:text-foreground font-medium'
    );
  }

  protected onKeydown(event: KeyboardEvent, index: number) {
    const target = targetTabIndex(event.key, index, this.tabs().length);

    if (target === null) return;

    event.preventDefault();

    const tab = this.tabs()[target];

    this.value.set(tab.value);
    this.tabButtons()[target]?.nativeElement.focus();
  }

  protected badgeClass(tabValue: string | number | null): string {
    return cn(
      'inline-flex h-5 min-w-5 items-center justify-center rounded-full px-1 text-xs font-medium',
      this.value() === tabValue
        ? 'bg-primary text-primary-foreground'
        : 'bg-foreground/10 text-foreground'
    );
  }
}

function targetTabIndex(
  key: string,
  index: number,
  count: number
): number | null {
  switch (key) {
    case 'ArrowRight':
      return (index + 1) % count;
    case 'ArrowLeft':
      return (index - 1 + count) % count;
    case 'Home':
      return 0;
    case 'End':
      return count - 1;
    default:
      return null;
  }
}
