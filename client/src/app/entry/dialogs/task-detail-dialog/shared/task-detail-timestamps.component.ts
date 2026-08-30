import { Component, inject, input } from '@angular/core';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { TaskDetailService } from '../task-detail.service';

@Component({
  selector: 'app-task-detail-timestamps',
  imports: [AvatarComponent, FromNowPipe, PrettyDatePipe, TooltipDirective],
  host: { class: 'text-muted flex items-center gap-4 text-xs' },
  template: `
    @if (task(); as task) {
      <span [appTooltip]="task.createdAt | prettyDate">
        <span
          i18n="
            Footer line naming when a task was created. WHEN is a relative time
            such as a month ago
          ">
          Created
          {{
            task.createdAt | fromNow // i18n(ph="WHEN")
          }}
        </span>
      </span>
      <span [appTooltip]="task.updatedAt | prettyDate">
        <span
          i18n="
            Footer line naming when a task last changed. WHEN is a relative time
            such as 4 days ago
          ">
          Updated
          {{
            task.updatedAt | fromNow // i18n(ph="WHEN")
          }}
        </span>
      </span>

      @if (showReporter()) {
        <span class="text-muted ml-auto flex items-center gap-2">
          <app-avatar
            size="xs"
            [tooltip]="false"
            [name]="task.ownerUsername"
            [imageUrl]="task.ownerPictureUrl"
            [isServiceAccount]="task.ownerIsServiceAccount ?? false" />
          <span
            i18n="
              Footer line naming who raised the task. NAME is their display name
            ">
            Reported by
            {{
              task.ownerUsername // i18n(ph="NAME")
            }}
          </span>
        </span>
      }
    }
  `,
})
export class TaskDetailTimestampsComponent {
  readonly showReporter = input(false);

  readonly task = inject(TaskDetailService).task;
}
