import { Component, computed, input, output } from '@angular/core';
import { GridCellWidget } from '@angular/aria/grid';
import { ScheduledTask } from '@core/models/scheduled-task';
import { StatusCategory } from '@core/models/status';
import { calendarTaskDragType } from '../../models/calendar.models';
import { taskEndsOn, taskStartsOn } from '../../utils/calendar-tasks';

@Component({
  selector: 'app-calendar-task-item',
  imports: [GridCellWidget],
  host: { class: 'block' },
  template: `
    <button
      ngGridCellWidget
      type="button"
      class="calendar-task border-border bg-card hover:bg-primary/8 focus-visible:ring-ring flex h-6 min-w-0 items-center gap-1 overflow-hidden border px-1.5 text-left text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
      [class.calendar-task--continues-before]="continuesBefore()"
      [class.calendar-task--continues-after]="continuesAfter()"
      [class.rounded-l-sm]="!continuesBefore()"
      [class.rounded-r-sm]="!continuesAfter()"
      [class.border-l-4]="!continuesBefore()"
      [class.border-l-0]="continuesBefore()"
      [class.border-r-0]="continuesAfter()"
      [class.opacity-60]="completed()"
      [style.border-left-color]="!continuesBefore() ? task().statusColor : null"
      [attr.aria-label]="ariaLabel()"
      [attr.title]="ariaLabel()"
      [attr.draggable]="editable()"
      (click)="taskSelected.emit(task())"
      (dragstart)="startDrag($event)">
      <span class="text-muted shrink-0 font-mono">
        {{ task().systemId }}
      </span>
      <span class="text-foreground truncate">{{ task().name }}</span>
    </button>
  `,
  styles: `
    .calendar-task {
      --calendar-task-before-extension: 0rem;
      --calendar-task-after-extension: 0rem;

      margin-left: calc(var(--calendar-task-before-extension) * -1);
      width: calc(
        100% + var(--calendar-task-before-extension) +
          var(--calendar-task-after-extension)
      );
    }

    .calendar-task--continues-before {
      --calendar-task-before-extension: 0.25rem;
    }

    .calendar-task--continues-after {
      --calendar-task-after-extension: 0.25rem;
    }

    @media (min-width: 40rem) {
      .calendar-task--continues-before {
        --calendar-task-before-extension: 0.375rem;
      }

      .calendar-task--continues-after {
        --calendar-task-after-extension: 0.375rem;
      }
    }
  `,
})
export class CalendarTaskItemComponent {
  readonly task = input.required<ScheduledTask>();
  readonly date = input.required<string>();
  readonly editable = input(false);
  readonly taskSelected = output<ScheduledTask>();

  readonly continuesBefore = computed(() => {
    const start = taskStartsOn(this.task());
    return !!start && start < this.date();
  });
  readonly continuesAfter = computed(() => {
    const end = taskEndsOn(this.task());
    return !!end && end > this.date();
  });
  readonly completed = computed(
    () => this.task().statusCategory === StatusCategory.done
  );
  readonly ariaLabel = computed(() => {
    const task = this.task();
    const start = taskStartsOn(task);
    const end = taskEndsOn(task);
    const schedule = start === end ? `on ${start}` : `from ${start} to ${end}`;
    return `${task.systemId}, ${task.name}, ${task.statusName}, ${schedule}`;
  });

  protected startDrag(event: DragEvent): void {
    if (!this.editable() || !event.dataTransfer) {
      event.preventDefault();
      return;
    }

    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData(
      calendarTaskDragType,
      JSON.stringify({ taskId: this.task().id, fromDate: this.date() })
    );
  }
}
