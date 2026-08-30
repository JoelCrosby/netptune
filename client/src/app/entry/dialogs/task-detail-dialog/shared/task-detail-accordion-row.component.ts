import { Component, input, output } from '@angular/core';
import { LucideChevronRight } from '@lucide/angular';

@Component({
  selector: 'app-task-detail-accordion-row',
  imports: [LucideChevronRight],
  host: { class: 'block' },
  template: `
    <div
      class="border-foreground/8 flex h-[46px] items-center gap-2"
      [class.border-b]="!last()">
      <button
        type="button"
        class="hover:bg-hover flex h-full min-w-0 flex-1 cursor-pointer items-center gap-2.5 rounded px-1 text-left transition-colors"
        [attr.aria-expanded]="expanded()"
        (click)="toggled.emit()">
        <svg
          lucideChevronRight
          class="text-muted h-3.5 w-3.5 shrink-0 transition-transform"
          [class.rotate-90]="expanded()"></svg>
        <span class="shrink-0 text-[13px] font-semibold">{{ label() }}</span>
        <span class="text-muted truncate text-xs">{{ summary() }}</span>
      </button>

      <ng-content />
    </div>
  `,
})
export class TaskDetailAccordionRowComponent {
  readonly label = input.required<string>();
  readonly summary = input('');

  readonly expanded = input(false);
  readonly last = input(false);

  readonly toggled = output();
}
