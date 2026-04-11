import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { PrettyDatePipe } from '../../pipes/pretty-date.pipe';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FromNowPipe } from '@static/pipes/from-now.pipe';

@Component({
  selector: 'app-task-dates',
  imports: [PrettyDatePipe, MatTooltipModule, FromNowPipe],
  template: `
    <div class="mr-auto flex flex-row items-center gap-4">
      <div
        class="mr-2 cursor-pointer text-sm"
        [matTooltip]="task().createdAt | prettyDate">
        <span class="mr-1 text-xs font-normal opacity-60">Created</span>
        <span class="font-medium">{{ task().createdAt | fromNow }}</span>
      </div>
      <div
        class="mr-2 cursor-pointer text-sm"
        [matTooltip]="task().updatedAt | prettyDate">
        <span class="mr-1 text-xs font-normal opacity-60">Updated</span>
        <span class="mr-2 font-medium">{{ task().updatedAt | fromNow }}</span>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDates {
  task = input.required<TaskViewModel>();
}
