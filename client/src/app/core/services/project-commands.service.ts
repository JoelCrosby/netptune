import { inject, Service, signal } from '@angular/core';
import { AddProjectRequest } from '@core/models/project';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { ConfirmationService } from '@core/services/confirmation.service';
import { ProjectsService } from '@core/services/projects.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { EMPTY, finalize, switchMap } from 'rxjs';

@Service()
export class ProjectCommandsService {
  private readonly projects = inject(ProjectsService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly updating = signal(false);

  readonly isUpdating = this.updating.asReadonly();

  create(request: AddProjectRequest) {
    this.projects
      .post(request)
      .pipe(unwrapClientReposne())
      .subscribe(() => this.workspaceRefresh.refresh(['projects']));
  }

  update(request: UpdateProjectRequest) {
    this.updating.set(true);

    this.projects
      .put(request)
      .pipe(
        unwrapClientReposne(),
        finalize(() => this.updating.set(false))
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Project updated`
        );
        this.workspaceRefresh.refresh(['projects']);
      });
  }

  delete(project: ProjectViewModel) {
    this.confirmation
      .open(DELETE_PROJECT_CONFIRMATION)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.projects.delete(project).pipe(unwrapClientReposne());
        })
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Project deleted`
        );
        this.workspaceRefresh.refresh(['projects', 'boards', 'tasks']);
      });
  }
}

const DELETE_PROJECT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Delete Project`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  color: 'warn',
  title: $localize`:Title of a confirmation dialog:Delete Project`,
  message: $localize`:Body of a confirmation dialog:Are you sure you wish to delete this project`,
};
