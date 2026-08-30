import { Component, computed, inject, input } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { EntityType } from '@core/models/entity-type';
import {
  ActivityFeedRequest,
  activityResource,
} from '@core/resources/activity.resource';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { ActivityPipe } from '@static/pipes/activity.pipe';
import { ActivityTimeRangePipe } from '@static/pipes/activity-time-range.pipe';
import { LucideActivity } from '@lucide/angular';
import { TaskDetailService } from '../task-detail.service';

@Component({
  selector: 'app-task-detail-activity-feed',
  imports: [
    AvatarComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    SpinnerComponent,
    TooltipDirective,
    ActivityPipe,
    ActivityTimeRangePipe,
    LucideActivity,
  ],
  host: { class: 'flex flex-col gap-3' },
  template: `
    @if (!loaded()) {
      <div class="flex justify-center p-4">
        <app-spinner diameter="24" />
      </div>
    } @else {
      @for (activity of activities(); track activity.id) {
        <div class="flex items-start gap-2.5">
          <app-avatar
            class="shrink-0"
            size="sm"
            [tooltip]="false"
            [imageUrl]="activity.userPictureUrl"
            [name]="activity.userUsername"
            [isServiceAccount]="activity.userIsServiceAccount ?? false" />
          <div class="min-w-0 text-[13px]/[20px]">
            <span class="font-semibold">{{ activity.userUsername }}</span>
            @if (activity.agent) {
              <span
                class="text-foreground/60 ml-1 text-xs"
                i18n="
                  Precedes the assistant that made a change on the user's behalf
                ">
                via {{ activity.agent }}
              </span>
            }
            <span
              class="text-muted ml-1"
              [appTooltip]="activity | activityTimeRange">
              {{ activity | activity }}
            </span>
          </div>
        </div>
      } @empty {
        <app-empty-state
          compact
          i18n-title="Heading of the empty activity feed"
          title="There is no activity"
          i18n-description="Explains why the activity feed is empty"
          description="Activity on the item will appear here">
          <svg emptyStateIcon lucideActivity></svg>
        </app-empty-state>
      }

      @if (canLoadMore()) {
        <div class="flex justify-center pt-1">
          <button app-flat-button color="ghost" (click)="loadMore()">
            <span i18n="Button that loads the next page of activity">
              Load more
            </span>
          </button>
        </div>
      }
    }
  `,
})
export class TaskDetailActivityFeedComponent {
  readonly enabled = input(true);

  private readonly taskDetail = inject(TaskDetailService);
  readonly readActivity = hasPermission(PERMISSIONS.activity.read);

  private readonly request = computed<ActivityFeedRequest | null>(() => {
    const entityId = this.taskDetail.task()?.id;

    if (!this.enabled() || entityId === undefined) return null;

    return { entityType: EntityType.task, entityId };
  });

  private readonly resource = activityResource(this.request);

  readonly activities = this.resource.items;
  readonly loaded = this.resource.loaded;
  readonly canLoadMore = this.resource.canLoadMore;

  loadMore() {
    this.resource.loadMore();
  }
}
