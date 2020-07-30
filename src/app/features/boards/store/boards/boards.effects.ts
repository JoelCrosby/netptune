import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SelectCurrentWorkspace } from '@app/core/store/workspaces/workspaces.selectors';
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
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
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

  deleteBoards$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteBoard),
      switchMap((action) =>
        this.boardsService.delete(action.board).pipe(
          tap(() => this.snackbar.open('Board Deleted')),
          map((board) => actions.deleteBoardSuccess({ board })),
          catchError((error) => of(actions.deleteBoardFail({ error })))
        )
      )
    )
  );

  onWorkspaceSelected$ = createEffect(() =>
    this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState))
  );

  constructor(
    private actions$: Actions<Action>,
    private boardsService: BoardsService,
    private store: Store,
    private snackbar: MatSnackBar
  ) {}
}
