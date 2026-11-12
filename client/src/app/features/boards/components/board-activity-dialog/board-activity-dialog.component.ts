import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { EntityType } from '@core/models/entity-type';
import {
  ActivityFeedRequest,
  activityResource,
} from '@core/resources/activity.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { clock } from '@core/util/clock';
import { LucideActivity, LucideHistory } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogHeaderComponent } from '@static/components/dialog/dialog-header.component';
import { DialogSectionComponent } from '@static/components/dialog/dialog-section.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { EventTimelineComponent } from '@static/components/event-timeline/event-timeline.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { SegmentedControlComponent } from '@static/components/segmented-control/segmented-control.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import {
  activityCategory,
  activityCategoryCounts,
  activityCategoryOptions,
  ActivityTimelineCategory,
  activityTimelineGroups,
} from '@core/transforms/activity-timeline';

export interface BoardActivityDialogData {
  boardId: number;
  boardName: string;
}

@Component({
  selector: 'app-board-activity-dialog',
  imports: [
    DialogHeaderComponent,
    DialogSectionComponent,
    EmptyStateComponent,
    EventTimelineComponent,
    IconTileComponent,
    LucideActivity,
    SegmentedControlComponent,
    SpinnerComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'flex h-full min-h-0 flex-col' },
  template: `
    <app-dialog-header
      showCloseButton
      divider="none"
      i18n-heading="Title of the board activity dialog"
      heading="Activity"
      [description]="description()">
      <app-icon-tile dialogHeaderIcon [icon]="historyIcon" />
    </app-dialog-header>

    <div app-dialog-section divider="bottom" class="shrink-0 px-5 pb-3.5">
      <app-segmented-control
        wrap
        i18n-ariaLabel="Accessible label for the activity timeline type filter"
        ariaLabel="Filter activity by type"
        [options]="options()"
        [(value)]="category" />
    </div>

    <div class="custom-scroll flex min-h-0 flex-1 flex-col overflow-y-auto">
      @if (!loaded()) {
        <div class="flex flex-1 items-center justify-center p-8">
          <app-spinner diameter="24" />
        </div>
      } @else if (!activities().length) {
        <div class="flex flex-1 items-center justify-center">
          <app-empty-state
            i18n-title="Heading of the empty activity feed"
            title="There is no activity"
            i18n-description="Explains that the board's activity feed is empty"
            description="Changes to the board and its tasks will appear here">
            <svg emptyStateIcon lucideActivity></svg>
          </app-empty-state>
        </div>
      } @else {
        @if (groups().length) {
          <app-event-timeline
            [groups]="groups()"
            (subjectClicked)="dialogRef.close()" />
        } @else {
          <div class="flex flex-1 items-center justify-center">
            <app-empty-state
              compact
              i18n-title="
                Shown in an activity timeline when the type filter hides every
                loaded entry
              "
              title="No activity matches this filter" />
          </div>
        }

        @if (canLoadMore()) {
          <div class="flex justify-center pt-3.5 pb-4.5">
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
    </div>
  `,
})
export class BoardActivityDialogComponent {
  static readonly width = '720px';
  static readonly height = '740px';
  static readonly panelClass = 'app-full-height-dialog';

  protected readonly dialogRef = inject(DialogRef);
  private readonly data = inject<BoardActivityDialogData>(DIALOG_DATA);
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;
  private readonly now = clock(30_000);

  protected readonly historyIcon = LucideHistory;
  protected readonly category = signal<ActivityTimelineCategory>('all');

  private readonly request = signal<ActivityFeedRequest>({
    entityType: EntityType.board,
    entityId: this.data.boardId,
  });

  private readonly feed = activityResource(this.request);

  protected readonly activities = this.feed.items;
  protected readonly loaded = this.feed.loaded;
  protected readonly isLoading = this.feed.isLoading;
  protected readonly canLoadMore = this.feed.canLoadMore;

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
      scope: { kind: 'board', boardName: this.data.boardName },
      now: this.now(),
    });
  });

  protected readonly description = computed(() => {
    const name = this.data.boardName;
    const count = this.activities().length;
    const loaded = this.loaded();
    const canLoadMore = this.canLoadMore();

    if (!loaded) return name;

    if (canLoadMore) {
      return $localize`:Subtitle of the board activity dialog while older entries are still unloaded. BOARD is the board's name and COUNT is a number:${name}:BOARD: · latest ${count}:COUNT: events`;
    }

    if (count === 1) {
      return $localize`:Subtitle of the board activity dialog when it holds one entry. BOARD is the board's name:${name}:BOARD: · 1 event`;
    }

    return $localize`:Subtitle of the board activity dialog. BOARD is the board's name and COUNT is a number:${name}:BOARD: · ${count}:COUNT: events`;
  });

  protected loadMore() {
    this.feed.loadMore();
  }
}
