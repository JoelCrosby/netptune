import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { PrettyDatePipe } from '../../pipes/pretty-date.pipe';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FromNowPipe } from '@static/pipes/from-now.pipe';

@Component({
  selector: 'app-task-dates',
  imports: [PrettyDatePipe, MatTooltipModule, FromNowPipe],
  template: `
    <div class="task-dates">
      <div class="created-at" [matTooltip]="task().createdAt | prettyDate">
        <span class="label">Created</span>
        <span class="value">{{ task().createdAt | fromNow }}</span>
      </div>
      <div class="updated-at" [matTooltip]="task().updatedAt | prettyDate">
        <span class="label">Updated</span>
        <span class="value">{{ task().updatedAt | fromNow }}</span>
      </div>
    </div>
  `,
  styleUrls: ['./task-dates.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDates {
  task = input.required<TaskViewModel>();
}
