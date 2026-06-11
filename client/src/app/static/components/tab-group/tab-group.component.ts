import { Component, input, output } from '@angular/core';

export interface TabItem {
  label: string;
  value: string | number | null;
  badge?: number;
}

@Component({
  selector: 'app-tab-group',
  template: `
    <div class="border-border flex border-b" role="tablist">
      @for (tab of tabs(); track tab.value) {
        <button
          role="tab"
          type="button"
          [attr.aria-selected]="value() === tab.value"
          [class]="tabClass(tab.value)"
          (click)="changed.emit(tab.value)">
          {{ tab.label }}
          @if (tab.badge !== undefined) {
            <span [class]="badgeClass(tab.value)">{{ tab.badge }}</span>
          }
        </button>
      }
    </div>
  `,
})
export class TabGroupComponent {
  readonly tabs = input.required<TabItem[]>();
  readonly value = input<string | number | null>(null);
  readonly changed = output<string | number | null>();

  tabClass(tabValue: string | number | null): string {
    const base =
      'inline-flex items-center gap-2 border-b-2 px-3 pb-2.5 pt-1 text-sm font-medium transition-colors focus-visible:outline-none cursor-pointer';
    const active = 'border-primary text-foreground';
    const inactive =
      'border-transparent text-muted hover:text-foreground hover:border-border';
    return `${base} ${this.value() === tabValue ? active : inactive}`;
  }

  badgeClass(tabValue: string | number | null): string {
    const base =
      'inline-flex h-5 min-w-5 items-center justify-center rounded-full px-1 text-xs font-medium';
    const active = 'bg-primary text-white';
    const inactive = 'bg-foreground/10 text-foreground';
    return `${base} ${this.value() === tabValue ? active : inactive}`;
  }
}
