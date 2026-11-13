import { Component, computed, inject } from '@angular/core';
import { BoardGroupHiddenNoticeComponent } from '@boards/components/board-group-hidden-notice/board-group-hidden-notice.component';
import { ManageBoardGroupsDialogComponent } from '@boards/components/manage-board-groups-dialog/manage-board-groups-dialog.component';
import {
  hiddenGroupIdsForBoard,
  withBoardHiddenGroups,
} from '@boards/util/hidden-board-groups';
import { BOARDS_HIDDEN_GROUP_IDS } from '@core/models/user-preferences';
import { BoardViewService } from '@core/services/board-view.service';
import { DialogService } from '@core/services/dialog.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';

@Component({
  selector: 'app-board-group-columns',
  imports: [BoardGroupHiddenNoticeComponent],
  template: `
    <app-board-group-hidden-notice
      [count]="hiddenCount()"
      (manage)="onManageClicked()"
      (showAll)="onShowAllClicked()" />
  `,
})
export class BoardGroupColumnsComponent {
  private readonly preferences = inject(UserPreferencesService);
  private readonly dialog = inject(DialogService);
  private readonly board = inject(BoardViewService).board;

  protected readonly hiddenCount = computed(() => {
    const boardId = this.board()?.id;

    if (boardId === undefined) return 0;

    const value = this.preferences.effectiveValueFor(BOARDS_HIDDEN_GROUP_IDS);

    return hiddenGroupIdsForBoard(value, boardId).length;
  });

  onManageClicked() {
    this.dialog.open(ManageBoardGroupsDialogComponent, {
      width: '640px',
      maxWidth: 'calc(100vw - 32px)',
    });
  }

  onShowAllClicked() {
    const boardId = this.board()?.id;

    if (boardId === undefined) return;

    const value = this.preferences.effectiveValueFor(BOARDS_HIDDEN_GROUP_IDS);

    this.preferences
      .updateValue(
        BOARDS_HIDDEN_GROUP_IDS,
        'workspace',
        withBoardHiddenGroups(value, boardId, [])
      )
      .subscribe();
  }
}
