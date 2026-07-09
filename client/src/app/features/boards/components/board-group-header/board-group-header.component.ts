import { Component, computed, inject } from '@angular/core';
import { BoardGroupUsersComponent } from '@boards/components/board-group-users/board-group-users.component';
import { BoardGroupsSearchComponent } from '@boards/components/board-groups-search/board-groups-search.component';
import { BoardGroupsSelectionComponent } from '@boards/components/board-groups-selection/board-groups-selection.component';
import { BoardGroupStatusComponent } from '@boards/components/board-group-status/board-group-status.component';
import { BoardGroupHiddenNoticeComponent } from '@boards/components/board-group-hidden-notice/board-group-hidden-notice.component';
import { ManageBoardGroupsDialogComponent } from '@boards/components/manage-board-groups-dialog/manage-board-groups-dialog.component';
import { hiddenGroupIdsForBoard } from '@boards/util/hidden-board-groups';
import { TagFilterContainerComponent } from '@shared/components/tag-filter/tag-filter-container.component';
import { BoardGroupHeaderSeperatorComponent } from './board-group-header-seperator.component';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { selectSelectedBoard } from '@app/core/store/groups/board-groups.selectors';
import { BOARDS_HIDDEN_GROUP_IDS } from '@core/models/user-preferences';
import { DialogService } from '@core/services/dialog.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-board-group-header',
  imports: [
    BoardGroupsSearchComponent,
    BoardGroupUsersComponent,
    TagFilterContainerComponent,
    BoardGroupStatusComponent,
    BoardGroupsSelectionComponent,
    BoardGroupHeaderSeperatorComponent,
    BoardGroupHiddenNoticeComponent,
  ],
  template: `<div class="flex flex-row items-center gap-2">
    <app-board-group-header-seperator />
    <app-board-groups-search />
    <app-board-group-header-seperator />
    <app-board-group-users />

    @if (readTags()) {
      <app-board-group-header-seperator />
      <app-tag-filter-container />
    }

    @if (readStatus()) {
      <app-board-group-header-seperator />
      <app-board-group-status />
    }
    <app-board-groups-selection />

    <app-board-group-header-seperator />
    <app-board-group-hidden-notice
      [count]="hiddenCount()"
      (manage)="onManageGroupsClicked()" />
  </div> `,
})
export class BoardGroupHeaderComponent {
  readonly store = inject(Store);
  private preferences = inject(UserPreferencesService);
  private dialog = inject(DialogService);

  private board = this.store.selectSignal(selectSelectedBoard);

  hiddenCount = computed(() => {
    const boardId = this.board()?.id;

    if (boardId === undefined) return 0;

    const value = this.preferences.effectiveValueFor(BOARDS_HIDDEN_GROUP_IDS);

    return hiddenGroupIdsForBoard(value, boardId).length;
  });

  readStatus = this.store.selectSignal(
    selectHasPermission(netptunePermissions.statuses.read)
  );

  readTags = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tags.read)
  );

  onManageGroupsClicked() {
    this.dialog.open(ManageBoardGroupsDialogComponent, {
      width: '600px',
    });
  }
}
