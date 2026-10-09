import { inject, Service, signal } from '@angular/core';
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
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, defer, EMPTY, finalize, Observable, tap } from 'rxjs';

@Service()
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
        unwrapClientResponse(),
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
        unwrapClientResponse(),
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
        unwrapClientResponse(),
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
        unwrapClientResponse(),
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

  completeWithCarryOver(
    sprintId: number,
    carryOverSprintId?: number
  ): Observable<SprintViewModel> {
    return defer(() => {
      this.updating.set(true);

      return this.completeSprint(sprintId, carryOverSprintId);
    }).pipe(
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
        unwrapClientResponse(),
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
        unwrapClientResponse(),
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
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Task removed from sprint`
        );
        this.refresh();
      });
  }

  private completeSprint(
    sprintId: number,
    carryOverSprintId?: number
  ): Observable<SprintViewModel> {
    return this.sprints.complete(sprintId, carryOverSprintId).pipe(
      unwrapClientResponse(),
      tap(() =>
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Sprint completed`
        )
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
