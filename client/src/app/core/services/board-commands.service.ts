import { inject, Injectable } from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { Router } from '@angular/router';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { UpdateBoardRequest } from '@core/models/requests/update-board-request';
import { BoardsService } from '@core/services/boards.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { EMPTY, switchMap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BoardCommandsService {
  private readonly boards = inject(BoardsService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly router = inject(Router);

  private readonly workspaceIdentifier = inject(CurrentWorkspaceService).slug;

  create(request: AddBoardRequest) {
    this.boards
      .post(request)
      .pipe(unwrapClientReposne())
      .subscribe(() => this.workspaceRefresh.refresh(['boards']));
  }

  update(request: UpdateBoardRequest) {
    this.boards
      .put(request)
      .pipe(unwrapClientReposne())
      .subscribe(() =>
        this.workspaceRefresh.refresh(['boards', 'boardGroups'])
      );
  }

  delete(boardId: number) {
    this.confirmation
      .open(DELETE_BOARD_CONFIRMATION)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.boards.delete(boardId).pipe(unwrapClientReposne());
        })
      )
      .subscribe(() => this.onDeleted());
  }

  private onDeleted() {
    this.snackbar.open(
      $localize`:Confirmation shown after an action succeeds:Board Deleted`
    );
    this.workspaceRefresh.refresh(['boards', 'boardGroups']);

    void this.router.navigate(['/', this.workspaceIdentifier(), 'boards']);
  }
}

const DELETE_BOARD_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Delete`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to delete this Board?`,
  title: $localize`:Title of a confirmation dialog:Delete Board`,
  color: 'warn',
};
