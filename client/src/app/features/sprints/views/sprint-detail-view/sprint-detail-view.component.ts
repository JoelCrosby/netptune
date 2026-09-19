import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { CreateTaskDialogComponent } from '@app/entry/dialogs/create-task-dialog/create-task-dialog.component';
import { PERMISSIONS } from '@core/auth/permissions';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { ConfirmationService } from '@core/services/confirmation.service';
import { CurrentSprintService } from '@core/services/current-sprint.service';
import { DialogService } from '@core/services/dialog.service';
import { sprintDetailResource } from '@core/resources/sprint.resource';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { distinctUntilChanged, map } from 'rxjs/operators';
import { SprintIdentityComponent } from '@static/components/sprint-identity.component';
import { SprintDetailActionsComponent } from '../../components/sprint-detail-actions.component';
import { SprintStatsComponent } from '../../components/sprint-stats.component';
import { SprintTaskListComponent } from '../../components/sprint-task-list.component';
import { EditSprintDialogComponent } from '../../dialogs/edit-sprint-dialog.component';
import { SprintAddTaskDialogComponent } from '../../dialogs/sprint-add-task-dialog.component';
import { SprintCompletionDialogComponent } from '../../dialogs/sprint-completion-dialog.component';
import { PanelComponent } from '@static/components/panel.component';

@Component({
  selector: 'app-sprint-detail-view',
  imports: [
    ErrorStateComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    PanelComponent,
    SprintDetailActionsComponent,
    SprintIdentityComponent,
    SprintStatsComponent,
    SprintTaskListComponent,
  ],
  template: `
    <app-page-container
      followsWidthPreference
      [centerPage]="true"
      [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for a single sprint"
        title="Sprint" />

      @if (loading()) {
        <app-page-loading />
      } @else if (loadError(); as error) {
        <app-error-state
          [title]="
            error.status === 404
              ? 'This sprint could not be found'
              : 'This sprint could not be loaded'
          "
          [description]="
            error.status === 404
              ? 'It may have been deleted, or you may not have access to it.'
              : 'Check your connection and try again.'
          "
          [retryable]="error.status !== 404"
          (retry)="reload()" />
      } @else if (sprint(); as sprint) {
        <section class="flex flex-col gap-6">
          <header
            app-panel
            surface="card"
            class="flex flex-wrap items-start justify-between gap-x-4 gap-y-4 px-6 py-5">
            <app-sprint-identity
              class="min-w-0 flex-1"
              size="large"
              showGoal
              [headingLevel]="1"
              [sprint]="sprint" />

            <app-sprint-detail-actions
              [sprint]="sprint"
              (createTask)="onCreateTask(sprint)"
              (assignTasks)="onAddTasks(sprint)"
              (startSprint)="onStart(sprint.id)"
              (completeSprint)="onComplete(sprint)"
              (editSprint)="onEdit(sprint)"
              (deleteSprint)="onDelete(sprint)" />
          </header>

          <app-sprint-stats [sprint]="sprint" />

          <app-sprint-task-list
            [sprint]="sprint"
            [canManage]="canManageTasks()" />
        </section>
      }
    </app-page-container>
  `,
})
export class SprintDetailViewComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(DialogService);
  private confirmation = inject(ConfirmationService);

  readonly sprintId = signal<number | null>(null);
  private readonly sprintCommands = inject(SprintCommandsService);
  private readonly currentSprint = inject(CurrentSprintService);
  private readonly sprintResourceRef = sprintDetailResource(
    computed(() => this.sprintId() ?? undefined)
  );

  readonly sprint = this.sprintResourceRef.value;

  readonly loading = computed(() => {
    return this.sprintResourceRef.status() === 'loading';
  });

  readonly loadError = computed(() => {
    return this.sprintResourceRef.error() as HttpErrorResponse | undefined;
  });

  readonly canManageTasks = hasPermission(PERMISSIONS.sprints.manageTasks);

  constructor() {
    this.route.paramMap
      .pipe(
        map((params) => Number(params.get('id'))),
        distinctUntilChanged(),
        takeUntilDestroyed()
      )
      .subscribe((sprintId) => {
        if (Number.isFinite(sprintId) && sprintId > 0) {
          this.sprintId.set(sprintId);
        }
      });

    effect(() => this.currentSprint.set(this.sprint()));

    inject(DestroyRef).onDestroy(() => this.clearCurrentSprint());
  }

  private clearCurrentSprint() {
    const sprint = this.sprint();

    if (!sprint) return;

    this.currentSprint.clearIfCurrent(sprint.id);
  }

  reload() {
    this.sprintResourceRef.reload();
  }

  onEdit(sprint: SprintDetailViewModel) {
    this.dialog.open(EditSprintDialogComponent, {
      width: '520px',
      data: sprint,
    });
  }

  onCreateTask(sprint: SprintDetailViewModel) {
    this.dialog.open(CreateTaskDialogComponent, {
      width: CreateTaskDialogComponent.width,
      height: CreateTaskDialogComponent.height,
      panelClass: CreateTaskDialogComponent.panelClass,
      data: { projectId: sprint.projectId, sprintId: sprint.id },
    });
  }

  onAddTasks(sprint: SprintDetailViewModel) {
    if (!sprint.id) return;

    this.dialog.open(SprintAddTaskDialogComponent, {
      data: { sprintId: sprint.id, projectId: sprint.projectId },
    });
  }

  onStart(sprintId?: number) {
    if (!sprintId) return;
    this.sprintCommands.start(sprintId);
  }

  onComplete(sprint: SprintDetailViewModel) {
    if (!sprint.id) return;
    this.dialog.open(SprintCompletionDialogComponent, {
      width: '900px',
      data: sprint,
    });
  }

  onDelete(sprint: SprintDetailViewModel) {
    if (!sprint.id) return;

    this.confirmation
      .open({
        title: $localize`:Title of the confirmation dialog for deleting a sprint:Delete Sprint`,
        message: `Delete "${sprint.name}"? This cannot be undone.`,
        acceptLabel: $localize`:Confirms a destructive action:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .subscribe((confirmed) => {
        if (confirmed && sprint.id) {
          this.sprintCommands.delete(sprint.id);
          this.router.navigate(['../'], { relativeTo: this.route });
        }
      });
  }
}
