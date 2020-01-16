import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import * as actions from './workspaces.actions';
import { WorkspacesService } from './workspaces.service';

@Injectable()
export class WorkspacesEffects {
  loadWorkspaces$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadWorkspaces),
      switchMap(action =>
        this.workspacesService.get().pipe(
          map(workspaces => actions.loadWorkspacesSuccess({ workspaces })),
          catchError(error => of(actions.loadWorkspacesFail({ error })))
        )
      )
    )
  );

  createWorkspace$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createWorkspace),
      switchMap(action =>
        this.workspacesService.post(action.workspace).pipe(
          map(workspace => actions.createWorkspaceSuccess({ workspace })),
          catchError(error => of(actions.createWorkspaceFail({ error })))
        )
      )
    )
  );

  deleteWorkspace$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteWorkspace),
      switchMap(action =>
        this.workspacesService.delete(action.workspace).pipe(
          map(workspace => actions.deleteWorkspaceSuccess({ workspace })),
          catchError(error => of(actions.deleteWorkspaceFail({ error })))
        )
      )
    )
  );

  editWorkspace$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.editWorkspace),
      switchMap(action =>
        this.workspacesService.put(action.workspace).pipe(
          map(workspace => actions.editWorkspaceSuccess({ workspace })),
          catchError(error => of(actions.editWorkspaceFail({ error })))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private workspacesService: WorkspacesService
  ) {}
}
