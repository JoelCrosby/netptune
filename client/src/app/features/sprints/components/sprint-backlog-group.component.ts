import { LowerCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TaskStatus } from '@core/enums/project-task-status';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { CardComponent } from '@static/components/card/card.component';
import { SprintBacklogTaskRowComponent } from './sprint-backlog-task-row.component';

export interface BacklogGroup {
  label: string;
  status: TaskStatus;
  tasks: TaskViewModel[];
}

@Component({
  selector: 'app-sprint-backlog-group',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LowerCasePipe, CardComponent, SprintBacklogTaskRowComponent],
  template: `
    <div class="flex flex-col gap-2">
      <div class="flex items-center gap-2">
        <h2 class="text-sm font-semibold tracking-wide uppercase">
          {{ group().label }}
        </h2>
        <span class="bg-muted rounded-full px-2 py-0.5 text-xs font-medium">
          {{ group().tasks.length }}
        </span>
      </div>

      <div class="bg-board-group p-2">
        <app-card class="min-h-0! p-0!">
          @for (task of group().tasks; track task.id) {
            <app-sprint-backlog-task-row [task]="task" [sprints]="sprints()" />
          } @empty {
            <div class="text-muted p-6 text-center text-sm">
              No {{ group().label | lowercase }} tasks in the backlog.
            </div>
          }
        </app-card>
      </div>
    </div>
  `,
})
export class SprintBacklogGroupComponent {
  readonly group = input.required<BacklogGroup>();
  readonly sprints = input.required<SprintViewModel[]>();
}
