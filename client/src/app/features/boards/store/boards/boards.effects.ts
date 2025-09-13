import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import {
  catchError,
  map,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import * as actions from './boards.actions';
import { BoardsService } from './boards.service';

@Injectable()
export class BoardsEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private boardsService = inject(BoardsService);
  private store = inject(Store);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(MatSnackBar);
  private router = inject(Router);

  loadBoards$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadBoards),
      switchMap(() =>
        this.boardsService.getByWorkspace().pipe(
          map((boards) => actions.loadBoardsSuccess({ boards })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadBoardsFail({ error }))
          )
        )
      )
    )
  );

  createBoard$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createBoard),
      switchMap((action) =>
        this.boardsService.post(action.request).pipe(
          unwrapClientReposne(),
          map((response) => actions.createBoardSuccess({ response })),
          catchError((error: HttpErrorResponse) =>
            of(actions.createBoardFail({ error }))
          )
        )
      )
    )
  );

  updateBoard$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.updateBoard),
      switchMap((action) =>
        this.boardsService.put(action.request).pipe(
          unwrapClientReposne(),
          map((response) => actions.updateBoardSuccess({ response })),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateBoardFail({ error }))
          )
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
              unwrapClientReposne(),
              tap(() => this.snackbar.open('Board Deleted')),
              map(() =>
                actions.deleteBoardSuccess({
                  boardId: action.boardId,
                })
              ),
              catchError((error: HttpErrorResponse) =>
                of(actions.deleteBoardFail({ error }))
              )
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
        withLatestFrom(this.store.select(selectCurrentWorkspaceIdentifier)),
        tap(
          ([_, workspaceId]) =>
            void this.router.navigate(['/', workspaceId, 'boards'])
        )
      ),
    { dispatch: false }
  );

  createBoardSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createBoardSuccess),
      map(() => actions.loadBoards())
    )
  );

  onWorkspaceSelected$ = createEffect(() =>
    this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState))
  );
}

const DELETE_BOARD_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this Board?',
  title: 'Delete Board',
};
