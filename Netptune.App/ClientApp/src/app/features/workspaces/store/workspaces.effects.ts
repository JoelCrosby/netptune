import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, map, switchMap, switchAll } from 'rxjs/operators';
import {
  ActionLoadWorkspacesSuccess,
  WorkspacesActions,
  WorkspacesActionTypes,
  ActionLoadWorkspacesFail,
  ActionCreateWorkspacesSuccess,
  ActionCreateWorkspacesFail,
} from './workspaces.actions';
import { WorkspacesService } from './workspaces.service';

@Injectable()
export class WorkspacesEffects {
  constructor(
    private actions$: Actions<WorkspacesActions>,
    private workspacesService: WorkspacesService
  ) {}

  @Effect()
  loadWorkspaces$ = this.actions$.pipe(
    ofType(WorkspacesActionTypes.LoadWorkspaces),
    switchMap(action =>
      this.workspacesService.get().pipe(
        map(workspaces => new ActionLoadWorkspacesSuccess(workspaces)),
        catchError(error => of(new ActionLoadWorkspacesFail(error)))
      )
    )
  );

  @Effect()
  createWorkspace$ = this.actions$.pipe(
    ofType(WorkspacesActionTypes.CreateWorkspace),
    switchMap(action =>
      this.workspacesService.post(action.payload).pipe(
        map(workspace => new ActionCreateWorkspacesSuccess(workspace)),
        catchError(error => of(new ActionCreateWorkspacesFail(error)))
      )
    )
  );
}
