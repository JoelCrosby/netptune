import { inject, Injectable, signal } from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';
import { AddSprintRequest } from '@core/models/requests/add-sprint-request';
import { AddTasksToSprintRequest } from '@core/models/requests/add-tasks-to-sprint-request';
import { UpdateSprintRequest } from '@core/models/requests/update-sprint-request';
import { ConfirmationService } from '@core/services/confirmation.service';
import { SprintFilterService } from '@core/services/sprint-filter.service';
import { SprintsService } from '@core/services/sprints.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { getErrorMessage } from '@core/util/error-message';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  catchError,
  defer,
  EMPTY,
  finalize,
  forkJoin,
  Observable,
  of,
  switchMap,
  tap,
} from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SprintCommandsService {
  private readonly sprints = inject(SprintsService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly sprintFilter = inject(SprintFilterService);

  private readonly creating = signal(false);
  private readonly updating = signal(false);

  readonly isCreating = this.creating.asReadonly();
  readonly isUpdating = this.updating.asReadonly();

  create(request: AddSprintRequest) {
    this.creating.set(true);

    this.sprints
      .post(request)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY),
        finalize(() => this.creating.set(false))
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Sprint created`
        );
        this.refresh();
      });
  }

  update(request: UpdateSprintRequest) {
    this.updating.set(true);

    this.sprints
      .put(request)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY),
        finalize(() => this.updating.set(false))
      )
      .subscribe((sprint) => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Sprint updated`
        );
        this.onSprintChanged(sprint);
      });
  }

  delete(sprintId: number) {
    this.sprints
      .delete(sprintId)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Sprint deleted`
        );
        this.sprintFilter.clearIfSelected(sprintId);
        this.refresh();
      });
  }

  /* Starting can fail for reasons the user must read, so it reports in a dialog. */
  start(sprintId: number) {
    this.updating.set(true);

    this.sprints
      .start(sprintId)
      .pipe(
        unwrapClientReposne(),
        catchError((error: unknown) => {
          void this.confirmation.open({
            title: $localize`:Title of a confirmation dialog:Unable to Start Sprint`,
            message: getErrorMessage(error, START_SPRINT_ERROR_FALLBACK),
            isInfoMessage: true,
          });

          return EMPTY;
        }),
        finalize(() => this.updating.set(false))
      )
      .subscribe((sprint) => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Sprint started`
        );
        this.onSprintChanged(sprint);
      });
  }

  complete(sprintId: number) {
    this.completeSprint(sprintId)
      .pipe(catchError(() => EMPTY))
      .subscribe((sprint) => this.onSprintChanged(sprint));
  }

  completeWithReassignment(
    sprintId: number,
    incompleteTaskIds: number[],
    targetSprintId?: number
  ): Observable<SprintViewModel> {
    return defer(() => {
      this.updating.set(true);

      return this.reassignIncompleteTasks(
        sprintId,
        incompleteTaskIds,
        targetSprintId
      );
    }).pipe(
      switchMap(() => this.completeSprint(sprintId)),
      catchError((error: unknown) => {
        this.snackbar.error(
          getErrorMessage(error, COMPLETE_SPRINT_ERROR_FALLBACK)
        );

        return EMPTY;
      }),
      tap((sprint) => this.onSprintChanged(sprint)),
      finalize(() => this.updating.set(false))
    );
  }

  addTasks(sprintId: number, request: AddTasksToSprintRequest) {
    this.sprints
      .addTasks(sprintId, request)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Tasks added to sprint`
        );
        this.refresh();
      });
  }

  addTask(sprintId: number, taskId: number) {
    this.sprints
      .addTasks(sprintId, { taskIds: [taskId] })
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Task added to sprint`
        );
        this.refresh();
      });
  }

  removeTask(sprintId: number, taskId: number) {
    this.sprints
      .removeTask(sprintId, taskId)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Task removed from sprint`
        );
        this.refresh();
      });
  }

  private completeSprint(sprintId: number): Observable<SprintViewModel> {
    return this.sprints.complete(sprintId).pipe(
      unwrapClientReposne(),
      tap(() =>
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Sprint completed`
        )
      )
    );
  }

  private reassignIncompleteTasks(
    sprintId: number,
    taskIds: number[],
    targetSprintId: number | undefined
  ): Observable<unknown> {
    if (taskIds.length === 0) return of(null);

    if (targetSprintId !== undefined) {
      return this.sprints
        .addTasks(targetSprintId, { taskIds })
        .pipe(unwrapClientReposne());
    }

    return forkJoin(
      taskIds.map((taskId) =>
        this.sprints.removeTask(sprintId, taskId).pipe(unwrapClientReposne())
      )
    );
  }

  /* A sprint that stops being active cannot stay the filter. */
  private onSprintChanged(sprint: SprintViewModel) {
    if (sprint.status !== SprintStatus.active) {
      this.sprintFilter.clearIfSelected(sprint.id);
    }

    this.refresh();
  }

  private refresh() {
    this.workspaceRefresh.refresh(['sprints', 'tasks']);
  }
}

const START_SPRINT_ERROR_FALLBACK =
  'The sprint could not be started. Please try again.';

const COMPLETE_SPRINT_ERROR_FALLBACK =
  'The sprint could not be completed. Please try again.';
