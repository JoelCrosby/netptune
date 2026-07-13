import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { CreateTaskDialogComponent } from '@app/entry/dialogs/create-task-dialog/create-task-dialog.component';
import { netptunePermissions } from '@core/auth/permissions';
import { SprintStatus } from '@core/enums/sprint-status';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import {
  deleteSprint,
  loadSprintDetail,
  startSprint,
} from '@core/store/sprints/sprints.actions';
import {
  selectSprintDetail,
  selectSprintDetailLoading,
  selectSprintUpdateLoading,
} from '@core/store/sprints/sprints.selectors';
import {
  LucideCheck,
  LucideListPlus,
  LucidePencil,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { distinctUntilChanged, map } from 'rxjs/operators';
import { SprintStatsComponent } from '../../components/sprint-stats.component';
import { SprintTaskListComponent } from '../../components/sprint-task-list.component';
import { EditSprintDialogComponent } from '../../dialogs/edit-sprint-dialog.component';
import { SprintAddTaskDialogComponent } from '../../dialogs/sprint-add-task-dialog.component';
import { SprintCompletionDialogComponent } from '../../dialogs/sprint-completion-dialog.component';
import { SprintStatusClassesPipe } from '../../pipes/sprint-status-classes.pipe';
import { SprintStatusLabelPipe } from '../../pipes/sprint-status-label.pipe';
import { sprintDaysChip } from '../../utils/sprint-days-chip';

@Component({
  imports: [
    DatePipe,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    FlatButtonComponent,
    IconButtonComponent,
    LucideListPlus,
    LucidePencil,
    LucidePlus,
    LucideTrash2,
    LucideCheck,
    SprintStatsComponent,
    SprintTaskListComponent,
    SprintStatusClassesPipe,
    SprintStatusLabelPipe,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Sprint" />

      @if (loading()) {
        <app-page-loading />
      } @else if (sprint(); as sprint) {
        <section class="flex flex-col gap-4">
          <div class="flex flex-wrap items-start justify-between gap-4">
            <div class="min-w-0 flex-1">
              <div class="mb-1 flex flex-wrap items-center gap-2">
                <h1 class="text-2xl font-semibold">{{ sprint.name }}</h1>
                <span
                  class="rounded-sm px-2.5 py-0.5 text-xs font-semibold"
                  [class]="sprint.status | sprintStatusClasses">
                  {{ sprint.status | sprintStatusLabel }}
                </span>
                @if (daysChip(sprint); as chip) {
                  <span
                    class="rounded-sm px-2 py-0.5 text-xs font-medium"
                    [class]="chip.classes">
                    {{ chip.label }}
                  </span>
                }
              </div>
              <p class="text-muted text-sm">
                <span class="font-medium">{{ sprint.projectName }}</span>
                &nbsp;·&nbsp;
                {{ sprint.startDate | date: 'mediumDate' }} –
                {{ sprint.endDate | date: 'mediumDate' }}
              </p>
            </div>

            <div class="flex shrink-0 items-center gap-2">
              @if (canUpdate()) {
                <button
                  app-icon-button
                  type="button"
                  title="Edit sprint"
                  (click)="onEdit(sprint)">
                  <svg lucidePencil class="h-4 w-4"></svg>
                </button>
                <button
                  app-icon-button
                  type="button"
                  title="Delete sprint"
                  (click)="onDelete(sprint)">
                  <svg lucideTrash2 class="h-4 w-4"></svg>
                </button>
              }

              @if (
                canManageTasks() && sprint.status !== sprintStatus.completed
              ) {
                <button
                  app-flat-button
                  color="neutral"
                  type="button"
                  title="Add existing tasks to this sprint"
                  (click)="onAddTasks(sprint)">
                  <svg lucideListPlus class="h-4 w-4"></svg>
                  Assign Existing Tasks
                </button>
                <button
                  app-flat-button
                  color="neutral"
                  type="button"
                  title="Create a new task in this sprint"
                  (click)="onCreateTask(sprint)">
                  <svg lucidePlus class="h-4 w-4"></svg>
                  Create Sprint Task
                </button>
              }

              @if (canUpdate() && sprint.status === sprintStatus.planning) {
                <button
                  app-flat-button
                  color="primary"
                  type="button"
                  [disabled]="updateLoading()"
                  (click)="onStart(sprint.id)">
                  Start Sprint
                </button>
              }

              @if (canUpdate() && sprint.status === sprintStatus.active) {
                <button
                  app-flat-button
                  color="primary"
                  type="button"
                  [disabled]="updateLoading()"
                  (click)="onComplete(sprint)">
                  <svg lucideCheck class="h-4 w-4"></svg>
                  Complete Sprint
                </button>
              }
            </div>
          </div>

          @if (sprint.goal) {
            <p class="text-muted text-sm">{{ sprint.goal }}</p>
          }

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
  private store = inject(Store);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(DialogService);
  private confirmation = inject(ConfirmationService);

  readonly sprintStatus = SprintStatus;
  readonly sprint = this.store.selectSignal(selectSprintDetail);
  readonly loading = this.store.selectSignal(selectSprintDetailLoading);
  readonly updateLoading = this.store.selectSignal(selectSprintUpdateLoading);
  readonly canUpdate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.update)
  );
  readonly canManageTasks = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.manageTasks)
  );

  constructor() {
    this.route.paramMap
      .pipe(
        map((params) => Number(params.get('id'))),
        distinctUntilChanged(),
        takeUntilDestroyed()
      )
      .subscribe((sprintId) => {
        if (Number.isFinite(sprintId) && sprintId > 0) {
          this.store.dispatch(loadSprintDetail.init({ sprintId }));
        }
      });
  }

  daysChip(sprint: SprintDetailViewModel) {
    return sprintDaysChip(sprint.status, sprint.endDate);
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
    this.store.dispatch(startSprint({ sprintId }));
  }

  onComplete(sprint: SprintDetailViewModel) {
    if (!sprint.id) return;
    this.dialog.open(SprintCompletionDialogComponent, {
      width: '520px',
      data: sprint,
    });
  }

  onDelete(sprint: SprintDetailViewModel) {
    if (!sprint.id) return;

    this.confirmation
      .open({
        title: 'Delete Sprint',
        message: `Delete "${sprint.name}"? This cannot be undone.`,
        acceptLabel: 'Delete',
        cancelLabel: 'Cancel',
        color: 'warn',
      })
      .subscribe((confirmed) => {
        if (confirmed && sprint.id) {
          this.store.dispatch(deleteSprint.init({ sprintId: sprint.id }));
          this.router.navigate(['../'], { relativeTo: this.route });
        }
      });
  }
}
