import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AppState } from '@core/core.state';
import { SelectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
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
import * as actions from './tasks.actions';
import { ProjectTasksService } from './tasks.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';

@Injectable()
export class ProjectTasksEffects {
  loadProjectTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadProjectTasks),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([action, workspace]) =>
        this.projectTasksService.get(workspace.slug).pipe(
          map((tasks) => actions.loadProjectTasksSuccess({ tasks })),
          catchError((error) => of(actions.loadProjectTasksFail(error)))
        )
      )
    )
  );

  createProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createProjectTask),
      switchMap((action) =>
        this.projectTasksService.post(action.task).pipe(
          tap(() => this.snackbar.open('Task created')),
          map((task) => actions.createProjectTasksSuccess({ task })),
          catchError((error) => of(actions.createProjectTasksFail({ error })))
        )
      )
    )
  );

  editProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.editProjectTask),
      switchMap((action) =>
        this.projectTasksService.put(action.task).pipe(
          tap(() => !!action.silent && this.snackbar.open('Task updated')),
          map((task) => actions.editProjectTasksSuccess({ task })),
          catchError((error) => of(actions.editProjectTasksFail({ error })))
        )
      )
    )
  );

  deleteProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteProjectTask),
      switchMap((action) =>
        this.projectTasksService.delete(action.task).pipe(
          tap(() => this.snackbar.open('Task deleted')),
          map((task) => actions.deleteProjectTasksSuccess({ task })),
          catchError((error) => of(actions.deleteProjectTasksFail({ error })))
        )
      )
    )
  );

  loadTaskDetail$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadTaskDetails),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([action, workspace]) =>
        this.projectTasksService.detail(action.systemId, workspace.slug).pipe(
          map((task) => actions.loadTaskDetailsSuccess({ task })),
          catchError((error) => of(actions.loadTaskDetailsFail({ error })))
        )
      )
    )
  );

  onWorkspaceSelected$ = createEffect(() =>
    this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState))
  );

  constructor(
    private actions$: Actions<Action>,
    private projectTasksService: ProjectTasksService,
    private snackbar: MatSnackBar,
    private store: Store<AppState>
  ) {}
}
