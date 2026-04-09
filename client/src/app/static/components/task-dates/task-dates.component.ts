import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { PrettyDatePipe } from '../../pipes/pretty-date.pipe';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FromNowPipe } from '@static/pipes/from-now.pipe';

@Component({
  selector: 'app-task-dates',
  imports: [PrettyDatePipe, MatTooltipModule, FromNowPipe],
  template: `
    <div class="flex flex-row items-center mr-auto gap-4">
      <div
        class="text-sm cursor-pointer mr-2"
        [matTooltip]="task().createdAt | prettyDate">
        <span class="opacity-60 text-xs font-normal mr-1">Created</span>
        <span class="font-medium">{{ task().createdAt | fromNow }}</span>
      </div>
      <div
        class="text-sm cursor-pointer mr-2"
        [matTooltip]="task().updatedAt | prettyDate">
        <span class="opacity-60 text-xs font-normal mr-1">Updated</span>
        <span class="font-medium mr-2">{{ task().updatedAt | fromNow }}</span>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDates {
  task = input.required<TaskViewModel>();
}
