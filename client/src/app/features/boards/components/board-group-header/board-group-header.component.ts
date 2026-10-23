import { Component, computed, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { BoardGroupUsersComponent } from '@boards/components/board-group-users/board-group-users.component';
import { BoardGroupsSearchComponent } from '@boards/components/board-groups-search/board-groups-search.component';
import { BoardGroupSortComponent } from '@boards/components/board-group-sort/board-group-sort.component';
import { BoardGroupStatusComponent } from '@boards/components/board-group-status/board-group-status.component';
import { BoardGroupHiddenNoticeComponent } from '@boards/components/board-group-hidden-notice/board-group-hidden-notice.component';
import { ManageBoardGroupsDialogComponent } from '@boards/components/manage-board-groups-dialog/manage-board-groups-dialog.component';
import { hiddenGroupIdsForBoard } from '@boards/util/hidden-board-groups';
import { TagFilterContainerComponent } from '@shared/components/tag-filter/tag-filter-container.component';
import { NotificationSubscribeComponent } from '@shared/components/notification-subscribe/notification-subscribe.component';
import { NotificationScope } from '@core/models/notification-subscription';
import { BoardGroupHeaderSeperatorComponent } from './board-group-header-seperator.component';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import { EntityType } from '@core/models/entity-type';
import { toolbarButtonVariants } from '@static/components/button/button.variants';
import { PERMISSIONS } from '@app/core/auth/permissions';
import { BoardViewService } from '@core/services/board-view.service';
import { BOARDS_HIDDEN_GROUP_IDS } from '@core/models/user-preferences';
import { DialogService } from '@core/services/dialog.service';
import { SessionService } from '@core/services/session.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';

@Component({
  selector: 'app-board-group-header',
  imports: [
    BoardGroupsSearchComponent,
    BoardGroupUsersComponent,
    TagFilterContainerComponent,
    BoardGroupStatusComponent,
    BoardGroupSortComponent,
    BoardGroupHeaderSeperatorComponent,
    BoardGroupHiddenNoticeComponent,
    NotificationSubscribeComponent,
    ActivityMenuComponent,
  ],
  template: `
    <div class="flex flex-row items-center gap-2">
      <app-board-group-header-seperator />
      <app-board-groups-search />
      <app-board-group-header-seperator />
      <app-board-group-users />

      <app-board-group-header-seperator />

      @if (readTags()) {
        <app-tag-filter-container />
      }

      @if (readStatus()) {
        <app-board-group-status />
      }

      <app-board-group-sort />

      @if (board(); as board) {
        @if (!isPublicViewer()) {
          <app-notification-subscribe
            appearance="toolbar"
            [scope]="notificationScope.board"
            [scopeEntityId]="board.id"
            [scopeName]="board.name" />
        }

        @if (readActivity()) {
          <app-activity-menu
            [entityType]="boardEntityType"
            [entityId]="board.id"
            [buttonClass]="activityButtonClass" />
        }
      }

      <app-board-group-hidden-notice
        [count]="hiddenCount()"
        (manage)="onManageGroupsClicked()" />
    </div>
  `,
})
export class BoardGroupHeaderComponent {
  private preferences = inject(UserPreferencesService);
  private dialog = inject(DialogService);

  protected readonly board = inject(BoardViewService).board;
  protected readonly isPublicViewer = inject(SessionService).isPublicViewer;

  protected readonly notificationScope = NotificationScope;
  protected readonly boardEntityType = EntityType.board;
  protected readonly activityButtonClass = toolbarButtonVariants({
    color: 'neutral',
  });
  protected readonly readActivity = hasPermission(PERMISSIONS.activity.read);

  hiddenCount = computed(() => {
    const boardId = this.board()?.id;

    if (boardId === undefined) return 0;

    const value = this.preferences.effectiveValueFor(BOARDS_HIDDEN_GROUP_IDS);

    return hiddenGroupIdsForBoard(value, boardId).length;
  });

  readStatus = hasPermission(PERMISSIONS.statuses.read);

  readTags = hasPermission(PERMISSIONS.tags.read);

  onManageGroupsClicked() {
    this.dialog.open(ManageBoardGroupsDialogComponent, {
      width: '640px',
      maxWidth: 'calc(100vw - 32px)',
    });
  }
}
