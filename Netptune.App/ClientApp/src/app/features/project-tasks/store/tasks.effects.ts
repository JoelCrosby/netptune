import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material';
import { AppState } from '@core/core.state';
import { SelectCurrentWorkspace } from '@core/state/core.selectors';
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

@Injectable()
export class ProjectTasksEffects {
  loadProjectTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadProjectTasks),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([action, workspace]) =>
        this.projectTasksService.get(workspace).pipe(
          map(tasks => actions.loadProjectTasksSuccess({ tasks })),
          catchError(error => of(actions.loadProjectTasksFail(error)))
        )
      )
    )
  );

  createProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createProjectTask),
      switchMap(action =>
        this.projectTasksService.post(action.task).pipe(
          tap(() => this.snackbar.open('Task created')),
          map(task => actions.createProjectTasksSuccess({ task })),
          catchError(error => of(actions.createProjectTasksFail({ error })))
        )
      )
    )
  );

  editProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.editProjectTask),
      switchMap(action =>
        this.projectTasksService.put(action.task).pipe(
          tap(() => this.snackbar.open('Task updated')),
          map(task => actions.editProjectTasksSuccess({ task })),
          catchError(error => of(actions.editProjectTasksFail({ error })))
        )
      )
    )
  );

  deleteProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteProjectTask),
      switchMap(action =>
        this.projectTasksService.delete(action.task).pipe(
          tap(() => this.snackbar.open('Task deleted')),
          map(task => actions.deleteProjectTasksSuccess({ task })),
          catchError(error => of(actions.deleteProjectTasksFail({ error })))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private projectTasksService: ProjectTasksService,
    private snackbar: MatSnackBar,
    private store: Store<AppState>
  ) {}
}
