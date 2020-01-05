import { loadBoardGroups } from './../groups/board-groups.actions';
import { selectCurrentBoard } from './boards.selectors';
import { selectCurrentProject } from '@core/projects/projects.selectors';
import { BoardsService } from './boards.service';
import { Injectable } from '@angular/core';
import { AppState } from '@core/core.state';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import {
  catchError,
  map,
  switchMap,
  withLatestFrom,
  tap,
  filter,
} from 'rxjs/operators';
import * as actions from './boards.actions';
import { MatSnackBar } from '@angular/material';

@Injectable()
export class BoardsEffects {
  loadBoards$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadBoards),
      withLatestFrom(this.store.select(selectCurrentProject)),
      filter(([_, project]) => project !== undefined),
      switchMap(([_, project]) =>
        this.boardsService.get(project.id).pipe(
          map(boards => actions.loadBoardsSuccess({ boards })),
          catchError(error => of(actions.loadBoardsFail({ error })))
        )
      )
    )
  );

  loadBoardsSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadBoardsSuccess),
      withLatestFrom(this.store.select(selectCurrentBoard)),
      map(([action, selected]) => {
        if (selected) return { type: '[N/A]' };
        return actions.selectBoard({
          board: action.boards && action.boards[0],
        });
      })
    )
  );

  createBoard$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createBoard),
      switchMap(action =>
        this.boardsService.post(action.board).pipe(
          map(board => actions.createBoardSuccess({ board })),
          catchError(error => of(actions.createBoardFail({ error })))
        )
      )
    )
  );

  deleteBoards$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteBoard),
      switchMap(action =>
        this.boardsService.delete(action.board).pipe(
          tap(() => this.snackbar.open('Board Deleted')),
          map(board => actions.deleteBoardSuccess({ board })),
          catchError(error => of(actions.deleteBoardFail({ error })))
        )
      )
    )
  );

  selectBoard$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.selectBoard),
      map(_ => loadBoardGroups())
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private boardsService: BoardsService,
    private store: Store<AppState>,
    private snackbar: MatSnackBar
  ) {}
}
