import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { EMPTY, of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';
import * as actions from './sprints.actions';
import { SprintsService } from './sprints.service';

@Injectable()
export class SprintsEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private sprintsService = inject(SprintsService);
  private snackbar = inject(SnackbarService);

  loadSprints$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadSprints),
      switchMap(({ filter }) =>
        this.sprintsService.get(filter).pipe(
          map((sprints) => actions.loadSprintsSuccess({ sprints, filter })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadSprintsFail({ error }))
          )
        )
      )
    );
  });

  loadProjectsForSprints$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadSprints),
      map(() => loadProjects())
    );
  });

  onWorkspaceSelected$ = createEffect(() => {
    return this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState));
  });

  loadCurrentSprints$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadCurrentSprints),
      switchMap(() =>
        this.sprintsService.get({ status: SprintStatus.active, take: 10 }).pipe(
          map((sprints) => actions.loadCurrentSprintsSuccess({ sprints })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadCurrentSprintsFail({ error }))
          )
        )
      )
    );
  });

  loadSprintDetail$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadSprintDetail),
      switchMap(({ sprintId }) =>
        this.sprintsService.detail(sprintId).pipe(
          unwrapClientReposne(),
          map((sprint) => actions.loadSprintDetailSuccess({ sprint })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadSprintDetailFail({ error }))
          )
        )
      )
    );
  });

  loadAvailableTasksForSprint$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadSprintDetailSuccess),
      switchMap(({ sprint }) => {
        if (sprint.status === SprintStatus.completed || !sprint.id)
          return EMPTY;

        return of(
          actions.loadAvailableSprintTasks({
            sprintId: sprint.id,
            projectId: sprint.projectId,
          })
        );
      })
    );
  });

  loadAvailableSprintTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadAvailableSprintTasks),
      switchMap(({ sprintId, projectId }) =>
        this.sprintsService.availableTasks(sprintId, projectId).pipe(
          map((tasks) => actions.loadAvailableSprintTasksSuccess({ tasks })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadAvailableSprintTasksFail({ error }))
          )
        )
      )
    );
  });

  createSprint$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createSprint),
      switchMap(({ request }) =>
        this.sprintsService.post(request).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Sprint created')),
          map((sprint) => actions.createSprintSuccess({ sprint })),
          catchError((error: HttpErrorResponse) =>
            of(actions.createSprintFail({ error }))
          )
        )
      )
    );
  });

  updateSprint$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.updateSprint),
      switchMap(({ request }) =>
        this.sprintsService.put(request).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Sprint updated')),
          map((sprint) => actions.updateSprintSuccess({ sprint })),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateSprintFail({ error }))
          )
        )
      )
    );
  });

  deleteSprint$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteSprint),
      switchMap(({ sprintId }) =>
        this.sprintsService.delete(sprintId).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Sprint deleted')),
          map(() => actions.deleteSprintSuccess({ sprintId })),
          catchError((error: HttpErrorResponse) =>
            of(actions.deleteSprintFail({ error }))
          )
        )
      )
    );
  });

  startSprint$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.startSprint),
      switchMap(({ sprintId }) =>
        this.sprintsService.start(sprintId).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Sprint started')),
          map((sprint) => actions.updateSprintSuccess({ sprint })),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateSprintFail({ error }))
          )
        )
      )
    );
  });

  completeSprint$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.completeSprint),
      switchMap(({ sprintId }) =>
        this.sprintsService.complete(sprintId).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Sprint completed')),
          map((sprint) => actions.updateSprintSuccess({ sprint })),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateSprintFail({ error }))
          )
        )
      )
    );
  });

  addTasksToSprint$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.addTasksToSprint),
      switchMap(({ sprintId, request }) =>
        this.sprintsService.addTasks(sprintId, request).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Tasks added to sprint')),
          map((sprint) => actions.loadSprintDetailSuccess({ sprint })),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateSprintFail({ error }))
          )
        )
      )
    );
  });

  removeTaskFromSprint$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.removeTaskFromSprint),
      switchMap(({ sprintId, taskId }) =>
        this.sprintsService.removeTask(sprintId, taskId).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Task removed from sprint')),
          map((sprint) => actions.loadSprintDetailSuccess({ sprint })),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateSprintFail({ error }))
          )
        )
      )
    );
  });
}
