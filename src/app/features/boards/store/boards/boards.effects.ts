import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import {
  catchError,
  filter,
  map,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import * as actions from './boards.actions';
import { BoardsService } from './boards.service';

@Injectable()
export class BoardsEffects {
  loadBoards$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadBoards),
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
      filter(([_, workspace]) => !!workspace),
      switchMap(([_, workspace]) =>
        this.boardsService.getByWorksapce(workspace.slug).pipe(
          map((boards) => actions.loadBoardsSuccess({ boards })),
          catchError((error) => of(actions.loadBoardsFail({ error })))
        )
      )
    )
  );

  createBoard$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createBoard),
      switchMap((action) =>
        this.boardsService.post(action.board).pipe(
          map((board) => actions.createBoardSuccess({ board })),
          catchError((error) => of(actions.createBoardFail({ error })))
        )
      )
    )
  );

  deleteBoard$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteBoard),
      switchMap((action) =>
        this.confirmation.open(DELETE_BOARD_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.boardsService.delete(action.boardId).pipe(
              tap(() => this.snackbar.open('Board Deleted')),
              map((board) => actions.deleteBoardSuccess({ board })),
              catchError((error) => of(actions.deleteBoardFail({ error })))
            );
          })
        )
      )
    )
  );

  deleteBoardSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(actions.deleteBoardSuccess),
        withLatestFrom(this.store.select(selectCurrentWorkspace)),
        tap(([_, workspace]) => {
          this.router.navigate(['/', workspace.slug, 'boards']);
        })
      ),
    { dispatch: false }
  );

  onWorkspaceSelected$ = createEffect(() =>
    this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState))
  );

  constructor(
    private actions$: Actions<Action>,
    private boardsService: BoardsService,
    private store: Store,
    private confirmation: ConfirmationService,
    private snackbar: MatSnackBar,
    private router: Router
  ) {}
}

const DELETE_BOARD_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this Board?',
  title: 'Delete Board',
};
