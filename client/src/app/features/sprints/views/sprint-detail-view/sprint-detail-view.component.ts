import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { CreateTaskDialogComponent } from '@app/entry/dialogs/create-task-dialog/create-task-dialog.component';
import { PERMISSIONS } from '@core/auth/permissions';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { ConfirmationService } from '@core/services/confirmation.service';
import { CurrentContextService } from '@core/services/current-context.service';
import { DialogService } from '@core/services/dialog.service';
import { sprintDetailResource } from '@core/resources/sprint.resource';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { distinctUntilChanged, map } from 'rxjs/operators';
import { SprintIdentityComponent } from '@shared/components/sprint-identity/sprint-identity.component';
import { SprintDetailActionsComponent } from '../../components/sprint-detail-actions.component';
import { SprintOverviewComponent } from '../../components/sprint-overview.component';
import { SprintTaskListComponent } from '../../components/sprint-task-list.component';
import { EditSprintDialogComponent } from '../../dialogs/edit-sprint-dialog.component';
import { SprintAddTaskDialogComponent } from '../../dialogs/sprint-add-task-dialog.component';
import { SprintCompletionDialogComponent } from '../../dialogs/sprint-completion-dialog.component';
import { sprintBurndownResource } from '@core/resources/reporting.resource';
import { hostTimeZone } from '@core/util/dates';
import { todaySignal } from '@core/util/today';
import { SprintStatus } from '@core/enums/sprint-status';
import { sprintHealth } from '../../sprint-health';
import { injectBreadcrumbs } from '@core/services/breadcrumb.service';

@Component({
  selector: 'app-sprint-detail-view',
  imports: [
    ErrorStateComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    SprintDetailActionsComponent,
    SprintIdentityComponent,
    SprintOverviewComponent,
    SprintTaskListComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for a single sprint"
        title="Sprint">
        @if (loadedSprint(); as sprint) {
          <app-sprint-detail-actions
            pageHeaderActions
            [sprint]="sprint"
            (createTask)="onCreateTask(sprint)"
            (assignTasks)="onAddTasks(sprint)"
            (startSprint)="onStart(sprint.id)"
            (completeSprint)="onComplete(sprint)"
            (editSprint)="onEdit(sprint)"
            (deleteSprint)="onDelete(sprint)" />
        }
      </app-page-header>

      <app-page-body scroll>
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
          <section class="flex flex-col gap-6 pb-16">
            <app-sprint-identity
              size="large"
              showGoal
              [headingLevel]="2"
              [sprint]="sprint"
              [detail]="dayLabel()" />

            <app-sprint-overview [sprint]="sprint" [burndown]="burndown()" />

            <app-sprint-task-list
              [sprint]="sprint"
              [addedTaskIds]="addedTaskIds()"
              [canManage]="canManageTasks()" />
          </section>
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class SprintDetailViewComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(DialogService);
  private confirmation = inject(ConfirmationService);

  readonly identifier = signal<string | null>(null);
  private readonly sprintCommands = inject(SprintCommandsService);
  private readonly currentContext = inject(CurrentContextService);
  private readonly sprintResourceRef = sprintDetailResource(
    computed(() => this.identifier() ?? undefined)
  );

  readonly sprint = this.sprintResourceRef.value;

  readonly loading = computed(() => {
    return this.sprintResourceRef.status() === 'loading';
  });

  readonly loadError = computed(() => {
    return this.sprintResourceRef.error() as HttpErrorResponse | undefined;
  });

  readonly canManageTasks = hasPermission(PERMISSIONS.sprints.manageTasks);

  readonly loadedSprint = computed(() => {
    const resource = this.sprintResourceRef;

    return resource.hasValue() ? resource.value() : undefined;
  });

  private readonly burndownResource = sprintBurndownResource(
    computed(() => this.loadedSprint()?.id),
    signal({ unit: 'Tasks', timeZone: hostTimeZone() })
  );

  // Fails quietly for a sprint that never started or a viewer without reporting
  // access, and reading an errored resource throws.
  readonly burndown = computed(() => {
    const resource = this.burndownResource;

    return resource.hasValue() ? resource.value() : undefined;
  });

  readonly addedTaskIds = computed(() => {
    return new Set(this.burndown()?.addedTaskIds ?? []);
  });

  private readonly today = todaySignal();

  readonly dayLabel = computed(() => {
    const sprint = this.loadedSprint();

    if (sprint?.status !== SprintStatus.active) return '';

    const { dayNumber, dayCount } = sprintHealth(
      sprint,
      undefined,
      this.today()
    );

    return $localize`:Position in an active sprint. DAY and TOTAL are day counts:Day ${Math.min(dayNumber, dayCount)}:DAY: of ${dayCount}:TOTAL:`;
  });

  constructor() {
    injectBreadcrumbs(
      () => this.sprint()?.name,
      () => this.loading()
    );

    this.route.paramMap
      .pipe(
        map((params) => params.get('identifier')),
        distinctUntilChanged(),
        takeUntilDestroyed()
      )
      .subscribe((identifier) => {
        if (identifier) {
          this.identifier.set(identifier);
        }
      });

    this.currentContext.publish('sprint', this.loadedSprint);
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
