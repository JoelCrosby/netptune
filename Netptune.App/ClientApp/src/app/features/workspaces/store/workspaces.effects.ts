import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import {
  createWorkspace,
  createWorkspaceFail,
  createWorkspaceSuccess,
  loadWorkspaces,
  loadWorkspacesFail,
  loadWorkspacesSuccess,
} from './workspaces.actions';
import { WorkspacesService } from './workspaces.service';

@Injectable()
export class WorkspacesEffects {
  loadWorkspaces$ = createEffect(() =>
    this.actions$.pipe(
      ofType(loadWorkspaces),
      switchMap(action =>
        this.workspacesService.get().pipe(
          map(workspaces => loadWorkspacesSuccess({ workspaces })),
          catchError(error => of(loadWorkspacesFail(error)))
        )
      )
    )
  );

  createWorkspace$ = createEffect(() =>
    this.actions$.pipe(
      ofType(createWorkspace),
      switchMap(action =>
        this.workspacesService.post(action.workspace).pipe(
          map(workspace => createWorkspaceSuccess({ workspace })),
          catchError(error => of(createWorkspaceFail(error)))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private workspacesService: WorkspacesService
  ) {}
}
