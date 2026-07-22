import { Component, computed, input, output } from '@angular/core';
import { GridCellWidget } from '@angular/aria/grid';
import { ScheduledTask } from '@core/models/scheduled-task';
import { StatusCategory } from '@core/models/status';
import { colorHex } from '@core/util/colors/colors';
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
      class="calendar-task focus-visible:ring-ring flex h-6 min-w-0 items-center gap-1 overflow-hidden border px-1.5 text-left text-sm transition-colors focus-visible:ring-2 focus-visible:outline-none"
      [class.calendar-task--continues-before]="continuesBefore()"
      [class.calendar-task--continues-after]="continuesAfter()"
      [class.rounded-l-sm]="!continuesBefore()"
      [class.rounded-r-sm]="!continuesAfter()"
      [class.border-l-4]="!continuesBefore()"
      [class.border-l-0]="continuesBefore()"
      [class.border-r-0]="continuesAfter()"
      [class.opacity-60]="completed()"
      [style.--calendar-task-color]="statusColor()"
      [style.border-left-color]="!continuesBefore() ? statusColor() : null"
      [attr.aria-label]="ariaLabel()"
      [attr.aria-keyshortcuts]="editable() ? 'M' : null"
      [attr.title]="ariaLabel()"
      [attr.draggable]="editable()"
      (click)="taskSelected.emit(task())"
      (keydown)="handleKeydown($event)"
      (dragstart)="startDrag($event)">
      @if (showLabel()) {
        <span class="text-muted shrink-0 text-xs">
          {{ task().systemId }}
        </span>
        <span class="text-foreground truncate">{{ task().name }}</span>
      }
    </button>
  `,
  styles: `
    .calendar-task {
      --calendar-task-before-extension: 0rem;
      --calendar-task-after-extension: 0rem;
      --calendar-task-color: var(--primary);

      position: relative;
      z-index: 2;
      margin-left: calc(var(--calendar-task-before-extension) * -1);
      border-color: color-mix(
        in srgb,
        var(--calendar-task-color) 32%,
        var(--border)
      );
      background-color: color-mix(
        in srgb,
        var(--calendar-task-color) 12%,
        var(--card)
      );
      width: calc(
        100% + var(--calendar-task-before-extension) +
          var(--calendar-task-after-extension)
      );
    }

    .calendar-task:hover {
      background-color: color-mix(
        in srgb,
        var(--calendar-task-color) 20%,
        var(--card)
      );
    }

    .calendar-task--continues-before {
      --calendar-task-before-extension: 0.375rem;
    }

    .calendar-task--continues-after {
      --calendar-task-after-extension: 0.375rem;
    }

    @media (min-width: 40rem) {
      .calendar-task--continues-before {
        --calendar-task-before-extension: 0.5rem;
      }

      .calendar-task--continues-after {
        --calendar-task-after-extension: 0.5rem;
      }
    }
  `,
})
export class CalendarTaskItemComponent {
  readonly task = input.required<ScheduledTask>();
  readonly date = input.required<string>();
  readonly dayIndex = input.required<number>();
  readonly editable = input(false);
  readonly showLabel = input(true);
  readonly taskSelected = output<ScheduledTask>();
  readonly moveRequested = output();
  readonly dragStarted = output();
  readonly statusColor = computed(() => colorHex(this.task().statusColor));

  readonly continuesBefore = computed(() => {
    const start = taskStartsOn(this.task());
    return this.dayIndex() > 0 && !!start && start < this.date();
  });
  readonly continuesAfter = computed(() => {
    const end = taskEndsOn(this.task());
    return this.dayIndex() < 6 && !!end && end > this.date();
  });
  readonly completed = computed(
    () => this.task().statusCategory === StatusCategory.done
  );
  readonly ariaLabel = computed(() => {
    const task = this.task();
    const start = taskStartsOn(task);
    const end = taskEndsOn(task);
    const schedule = start === end ? `on ${start}` : `from ${start} to ${end}`;
    const moveHint = this.editable() ? '. Press M to move this task' : '';
    return `${task.systemId}, ${task.name}, ${task.statusName}, ${schedule}${moveHint}`;
  });

  protected handleKeydown(event: KeyboardEvent): void {
    if (
      !this.editable() ||
      event.key.toLowerCase() !== 'm' ||
      event.altKey ||
      event.ctrlKey ||
      event.metaKey
    ) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();

    this.moveRequested.emit();
  }

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

    this.dragStarted.emit();
  }
}
