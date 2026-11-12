import { DialogRef } from '@angular/cdk/dialog';
import {
  Component,
  computed,
  inject,
  input,
  linkedSignal,
} from '@angular/core';
import { EntityType } from '@core/models/entity-type';
import {
  ActivityFeedRequest,
  activityResource,
} from '@core/resources/activity.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import {
  activityCategory,
  activityCategoryCounts,
  activityCategoryOptions,
  ActivityTimelineCategory,
  activityTimelineGroups,
} from '@core/transforms/activity-timeline';
import { clock } from '@core/util/clock';
import { LucideActivity } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { EventTimelineComponent } from '@static/components/event-timeline/event-timeline.component';
import { SegmentedControlComponent } from '@static/components/segmented-control/segmented-control.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { TaskDetailService } from '../task-detail.service';

@Component({
  selector: 'app-task-detail-activity-feed',
  imports: [
    EmptyStateComponent,
    EventTimelineComponent,
    LucideActivity,
    SegmentedControlComponent,
    SpinnerComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'flex flex-col gap-3' },
  template: `
    @if (!loaded()) {
      <div class="flex justify-center p-4">
        <app-spinner diameter="24" />
      </div>
    } @else if (!activities().length) {
      <app-empty-state
        compact
        i18n-title="Heading shown when a task has no history"
        title="There is no history for this task"
        i18n-description="Explains why the task's history is empty"
        description="Changes to the task will appear here">
        <svg emptyStateIcon lucideActivity></svg>
      </app-empty-state>
    } @else {
      <app-segmented-control
        wrap
        i18n-ariaLabel="Accessible label for the activity timeline type filter"
        ariaLabel="Filter activity by type"
        [options]="options()"
        [(value)]="category" />

      @if (groups().length) {
        <app-event-timeline
          class="-mx-4"
          [surface]="surface"
          [groups]="groups()" />
      } @else {
        <app-empty-state
          compact
          i18n-title="
            Shown in an activity timeline when the type filter hides every
            loaded entry
          "
          title="No activity matches this filter" />
      }

      @if (canLoadMore()) {
        <div class="flex justify-center pt-1">
          <button
            app-stroked-button
            color="neutral"
            size="small"
            [disabled]="isLoading()"
            (click)="loadMore()">
            <span
              i18n="
                Button at the end of an activity timeline that loads older
                entries
              ">
              Load older activity
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
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;
  private readonly now = clock(30_000);

  private readonly dialogRef = inject(DialogRef, { optional: true });

  protected readonly surface = this.dialogRef ? 'dialog' : 'background';

  protected readonly category = linkedSignal<
    number | undefined,
    ActivityTimelineCategory
  >({
    source: this.taskDetail.taskId,
    computation: () => 'all',
  });

  private readonly request = computed<ActivityFeedRequest | null>(() => {
    const entityId = this.taskDetail.taskId();

    if (!this.enabled() || entityId === undefined) return null;

    return { entityType: EntityType.task, entityId };
  });

  private readonly resource = activityResource(this.request);

  readonly activities = this.resource.items;
  readonly loaded = this.resource.loaded;
  readonly isLoading = this.resource.isLoading;
  readonly canLoadMore = this.resource.canLoadMore;

  private readonly counts = computed(() => {
    return activityCategoryCounts(this.activities());
  });

  protected readonly options = computed(() => {
    return activityCategoryOptions(this.counts(), this.category());
  });

  protected readonly groups = computed(() => {
    const category = this.category();
    const visible = this.activities().filter((activity) => {
      return category === 'all' || activityCategory(activity) === category;
    });

    return activityTimelineGroups(visible, {
      workspaceSlug: this.workspaceSlug(),
      scope: { kind: 'task' },
      now: this.now(),
    });
  });

  loadMore() {
    this.resource.loadMore();
  }
}
