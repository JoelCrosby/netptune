import { Component, computed, input } from '@angular/core';
import { StatusCategory } from '@core/models/status';
import { cn } from '../button/button.variants';
import { PopoverSurfaceComponent } from '../popover-surface/popover-surface.component';
import { TaskStatusPillComponent } from '../task-status-pill.component';

@Component({
  selector: 'app-task-preview-card',
  imports: [PopoverSurfaceComponent, TaskStatusPillComponent],
  host: { '[class]': 'hostClass()' },
  template: `
    <app-popover-surface
      size="compact"
      panelClass="flex w-[340px] flex-col gap-2.5 p-3.5 text-[13px] leading-snug">
      <span class="flex items-center gap-2">
        <span class="text-foreground/50 font-mono text-[11.5px]">{{
          systemId()
        }}</span>
        <span class="flex-1"></span>
        @if (statusName()) {
          <app-task-status-pill
            [name]="statusName()"
            [color]="statusColor()"
            [category]="statusCategory()" />
        }
      </span>
      <span class="text-sm font-semibold">{{ name() }}</span>
      @if (details().length) {
        <span class="text-foreground/50 flex flex-wrap gap-3 text-xs">
          @for (detail of details(); track $index) {
            <span>{{ detail }}</span>
          }
        </span>
      }
    </app-popover-surface>
  `,
})
export class TaskPreviewCardComponent {
  readonly systemId = input.required<string>();
  readonly name = input.required<string>();
  readonly statusName = input<string | null>(null);
  readonly statusColor = input<string | null>(null);
  readonly statusCategory = input<StatusCategory | null>(null);
  readonly sprintName = input<string | null>(null);
  readonly assigneeNames = input<readonly string[]>([]);
  readonly class = input('');

  protected readonly hostClass = computed(() => cn('block', this.class()));

  protected readonly details = computed(() => {
    const sprint = this.sprintName();
    const assignees = this.assigneeNames().join(', ');

    return [sprint, assignees].filter((detail): detail is string => {
      return Boolean(detail);
    });
  });
}
