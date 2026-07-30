import { Component, computed, input } from '@angular/core';
import { StatusCategory } from '@core/models/status';
import { colorPillClasses } from '@core/util/colors/colors';
import { LucideCheck } from '@lucide/angular';
import { BadgeComponent } from './badge/badge.component';

@Component({
  selector: 'app-task-status-pill',
  imports: [BadgeComponent, LucideCheck],
  template: `
    <app-badge [class]="'gap-1.5 leading-none ' + pillClasses()">
      @if (category() === statusCategory.done) {
        <svg lucideCheck class="h-3.5 w-3.5"></svg>
      }
      {{ name() }}
    </app-badge>
  `,
})
export class TaskStatusPillComponent {
  readonly name = input.required<string>();
  readonly color = input<string | null>();
  readonly category = input<StatusCategory | null>(null);

  readonly statusCategory = StatusCategory;

  readonly pillClasses = computed(() => {
    if (this.color()) {
      return colorPillClasses(this.color());
    }

    switch (this.category()) {
      case StatusCategory.todo:
        return 'bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-300';
      case StatusCategory.active:
        return 'bg-yellow-100 text-yellow-700 dark:bg-yellow-500/15 dark:text-yellow-300';
      case StatusCategory.done:
        return 'bg-green-100 text-green-700 dark:bg-green-500/15 dark:text-green-300';
      case StatusCategory.backlog:
        return 'bg-purple-100 text-purple-700 dark:bg-purple-500/15 dark:text-purple-300';
      default:
        return 'bg-neutral-100 text-neutral-600 dark:bg-neutral-500/15 dark:text-neutral-300';
    }
  });
}
