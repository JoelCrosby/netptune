import { inject, Service } from '@angular/core';
import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { DeleteTagFromTaskRequest } from '@core/models/requests/delete-tag-from-task-request';
import { ConfirmationService } from '@core/services/confirmation.service';
import { TagsService } from '@core/services/tags.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { catchError, EMPTY, switchMap } from 'rxjs';

@Service()
export class TagCommandsService {
  private readonly tags = inject(TagsService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  create(name: string) {
    this.tags
      .post({ tag: name })
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe(() => this.refresh());
  }

  rename(currentValue: string, newValue: string) {
    this.tags
      .patch({ currentValue, newValue })
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe(() => this.refresh());
  }

  delete(tags: string[]) {
    this.confirmation
      .open(DELETE_TAG_CONFIRMATION)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.tags.delete({ tags }).pipe(unwrapClientResponse());
        }),
        catchError(() => EMPTY)
      )
      .subscribe(() => this.refresh());
  }

  addToTask(request: AddTagToTaskRequest) {
    this.tags
      .postToTask(request)
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe(() => this.refresh());
  }

  removeFromTask(request: DeleteTagFromTaskRequest) {
    this.tags
      .deleteFromTask(request)
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe(() => this.refresh());
  }

  private refresh() {
    this.workspaceRefresh.refresh(['tags', 'tasks']);
  }
}

const DELETE_TAG_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Delete Tag`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  color: 'warn',
  title: $localize`:Title of a confirmation dialog:Delete Tag`,
  message: $localize`:Body of a confirmation dialog:Are you sure you wish to delete this tag`,
};
