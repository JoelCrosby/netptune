import { Component, input, model } from '@angular/core';

export interface TaskDetailTab {
  key: string;
  label: string;
  count?: number | null;
}

@Component({
  selector: 'app-task-detail-tabs',
  host: { class: 'flex items-center' },
  template: `
    @for (tab of tabs(); track tab.key) {
      <button
        type="button"
        role="tab"
        [attr.aria-selected]="tab.key === active()"
        [class]="tab.key === active() ? activeClass() : idleClass()"
        (click)="active.set(tab.key)">
        {{ tab.label }}
        @if (tab.count !== null && tab.count !== undefined) {
          <span class="text-muted ml-1.5 font-medium">
            {{ tab.count }}
          </span>
        }
      </button>
    }

    <ng-content />
  `,
})
export class TaskDetailTabsComponent {
  readonly tabs = input.required<TaskDetailTab[]>();
  readonly active = model.required<string>();
  readonly variant = input<'strip' | 'text'>('strip');

  protected base(): string {
    return this.variant() === 'strip'
      ? 'h-[42px] cursor-pointer border-b-2 px-3 text-[13px] transition-colors'
      : 'h-[30px] cursor-pointer border-b-2 text-[13px] transition-colors';
  }

  protected activeClass(): string {
    return `${this.base()} border-primary text-foreground font-semibold`;
  }

  protected idleClass(): string {
    return `${this.base()} text-muted hover:text-foreground border-transparent font-medium`;
  }
}
