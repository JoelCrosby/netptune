import { Service, inject, signal } from '@angular/core';
import {
  CreateTaskPinRequest,
  TaskPin,
  TaskPinOrder,
  TaskPinScope,
} from '@core/models/task-pin';
import { TaskPinsService } from '@core/services/task-pins.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, forkJoin } from 'rxjs';

@Service()
export class PinCommandsService {
  private readonly pinsApi = inject(TaskPinsService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly snackbar = inject(SnackbarService);

  private readonly scopeMenuRequest = signal(0);

  readonly scopeMenuRequested = this.scopeMenuRequest.asReadonly();

  requestScopeMenu() {
    this.scopeMenuRequest.update((request) => request + 1);
  }

  pin(request: CreateTaskPinRequest) {
    this.pinsApi
      .create(request)
      .pipe(catchError(() => this.reportFailure(this.pinFailedMessage())))
      .subscribe(() => this.refresh());
  }

  unpin(pin: TaskPin) {
    this.pinsApi
      .delete(pin.id)
      .pipe(catchError(() => this.reportFailure(this.unpinFailedMessage())))
      .subscribe(() => this.refresh());
  }

  unpinEverywhere(pins: TaskPin[]) {
    const removable = pins.filter((pin) => pin.canUnpin);

    if (!removable.length) return;

    const requests = removable.map((pin) => this.pinsApi.delete(pin.id));

    forkJoin(requests)
      .pipe(catchError(() => this.reportFailure(this.unpinFailedMessage())))
      .subscribe(() => this.refresh());
  }

  reorder(items: TaskPinOrder[]) {
    if (!items.length) return;

    this.pinsApi
      .reorder({ items })
      .pipe(catchError(() => this.reportFailure(this.reorderFailedMessage())))
      .subscribe(() => this.refresh());
  }

  pinForSelf(taskId: number) {
    this.pin({ taskId, scope: TaskPinScope.user });
  }

  private refresh() {
    this.workspaceRefresh.refresh(['pins']);
  }

  private reportFailure(message: string) {
    this.snackbar.error(message);

    return EMPTY;
  }

  private pinFailedMessage() {
    return $localize`:Error shown when a task could not be pinned:That task could not be pinned.`;
  }

  private unpinFailedMessage() {
    return $localize`:Error shown when a pin could not be removed:That pin could not be removed.`;
  }

  private reorderFailedMessage() {
    return $localize`:Error shown when pinned tasks could not be reordered:The pinned tasks could not be reordered.`;
  }
}
