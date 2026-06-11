import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import {
  unwrapClientPageReposne,
  unwrapClientReposne,
} from '@core/util/rxjs-operators';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { EMPTY, Observable, forkJoin, of } from 'rxjs';
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
          unwrapClientPageReposne(),
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

  completeSprintWithReassignment$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.completeSprintWithReassignment),
      switchMap(({ sprintId, incompleteTaskIds, targetSprintId }) =>
        this.reassignIncompleteTasks(
          sprintId,
          incompleteTaskIds,
          targetSprintId
        ).pipe(
          switchMap(() =>
            this.sprintsService.complete(sprintId).pipe(unwrapClientReposne())
          ),
          tap(() => this.snackbar.open('Sprint completed')),
          map((sprint) => actions.updateSprintSuccess({ sprint })),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateSprintFail({ error }))
          )
        )
      )
    );
  });

  private reassignIncompleteTasks(
    sprintId: number,
    taskIds: number[],
    targetSprintId: number | undefined
  ): Observable<unknown> {
    if (taskIds.length === 0) return of(null);

    if (targetSprintId !== undefined) {
      return this.sprintsService
        .addTasks(targetSprintId, { taskIds })
        .pipe(unwrapClientReposne());
    }

    return forkJoin(
      taskIds.map((taskId) =>
        this.sprintsService
          .removeTask(sprintId, taskId)
          .pipe(unwrapClientReposne())
      )
    );
  }

  initBacklogView$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.initBacklogView),
      switchMap(() => [
        actions.loadBacklogTasks(),
        actions.loadSprints({ filter: { take: 100 } }),
      ])
    );
  });

  assignBacklogTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.assignBacklogTask),
      switchMap(({ taskId, sprintId }) =>
        this.sprintsService.addTasks(sprintId, { taskIds: [taskId] }).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Task added to sprint')),
          switchMap((sprint) => [
            actions.loadSprintDetailSuccess({ sprint }),
            actions.removeTaskFromBacklog({ taskId }),
          ]),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateSprintFail({ error }))
          )
        )
      )
    );
  });

  loadBacklogTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadBacklogTasks),
      switchMap(() =>
        this.sprintsService.backlogTasks().pipe(
          unwrapClientPageReposne(),
          map((tasks) => actions.loadBacklogTasksSuccess({ tasks })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadBacklogTasksFail({ error }))
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
