import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { CreateTaskDialogComponent } from '@app/entry/dialogs/create-task-dialog/create-task-dialog.component';
import { PERMISSONS } from '@core/auth/permissions';
import { SprintStatus } from '@core/enums/sprint-status';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { sprintDetailResource } from '@core/resources/sprint.resource';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import {
  LucideCalendarClock,
  LucideCheck,
  LucideListPlus,
  LucideSettings2,
  LucidePlus,
  LucideSparkles,
  LucideTrash2,
} from '@lucide/angular';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { SprintDaysBadgeComponent } from '@static/components/sprint-days-badge.component';
import { SprintStatusBadgeComponent } from '@static/components/sprint-status-badge.component';
import { distinctUntilChanged, map } from 'rxjs/operators';
import { SprintStatsComponent } from '../../components/sprint-stats.component';
import { SprintTaskListComponent } from '../../components/sprint-task-list.component';
import { EditSprintDialogComponent } from '../../dialogs/edit-sprint-dialog.component';
import { SprintAddTaskDialogComponent } from '../../dialogs/sprint-add-task-dialog.component';
import { SprintCompletionDialogComponent } from '../../dialogs/sprint-completion-dialog.component';

@Component({
  selector: 'app-sprint-detail-view',
  imports: [
    DatePipe,
    ErrorStateComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    FlatButtonComponent,
    IconButtonComponent,
    LucideListPlus,
    LucideSettings2,
    LucidePlus,
    LucideSparkles,
    LucideTrash2,
    LucideCheck,
    SprintStatsComponent,
    SprintTaskListComponent,
    SprintStatusBadgeComponent,
    IconTileComponent,
    SprintDaysBadgeComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
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
            class="border-border bg-card flex flex-wrap items-start justify-between gap-x-4 gap-y-4 rounded-lg border px-6 py-5 shadow-sm">
            <div class="flex min-w-0 flex-1 items-start gap-3">
              <app-icon-tile [icon]="sprintIcon" />

              <div class="min-w-0">
                <div class="flex flex-wrap items-center gap-2">
                  <h1 class="font-overpass truncate text-xl font-semibold">
                    {{ sprint.name }}
                  </h1>
                  <app-sprint-status-badge [status]="sprint.status" />
                  <app-sprint-days-badge
                    [status]="sprint.status"
                    [endDate]="sprint.endDate" />
                </div>

                <p class="text-muted mt-1 text-sm">
                  <span class="font-medium">{{ sprint.projectName }}</span>
                  &nbsp;·&nbsp;
                  {{ sprint.startDate | date: 'mediumDate' }} –
                  {{ sprint.endDate | date: 'mediumDate' }}
                </p>

                @if (sprint.goal) {
                  <p class="text-muted mt-2 text-sm">{{ sprint.goal }}</p>
                }
              </div>
            </div>

            <div class="flex shrink-0 flex-wrap items-center gap-2">
              @if (assistant.isAvailable()) {
                <button
                  app-icon-button
                  type="button"
                  i18n-title="
                    Tooltip on the button that asks the assistant about this
                    sprint
                  "
                  title="Ask the assistant about this sprint"
                  (click)="assistant.askAboutSprint(sprint)">
                  <svg lucideSparkles class="h-4 w-4"></svg>
                </button>
              }

              @if (canUpdate()) {
                <button
                  app-icon-button
                  type="button"
                  i18n-title="Tooltip on the button that edits the sprint"
                  title="Edit sprint"
                  (click)="onEdit(sprint)">
                  <svg lucideSettings2 class="h-4 w-4"></svg>
                </button>
                <button
                  app-icon-button
                  type="button"
                  i18n-title="Tooltip on the button that deletes the sprint"
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
                  i18n-title="
                    Tooltip on the button that adds existing tasks to the sprint
                  "
                  title="Add existing tasks to this sprint"
                  (click)="onAddTasks(sprint)">
                  <svg lucideListPlus class="h-4 w-4"></svg>
                  <span
                    i18n="
                      Button that opens the dialog for adding existing tasks to
                      the sprint
                    ">
                    Assign Existing Tasks
                  </span>
                </button>
                <button
                  app-flat-button
                  color="neutral"
                  type="button"
                  i18n-title="
                    Tooltip on the button that creates a task in the sprint
                  "
                  title="Create a new task in this sprint"
                  (click)="onCreateTask(sprint)">
                  <svg lucidePlus class="h-4 w-4"></svg>
                  <span i18n="Button that creates a new task in the sprint">
                    Create Sprint Task
                  </span>
                </button>
              }

              @if (canUpdate() && sprint.status === sprintStatus.planning) {
                <button
                  app-flat-button
                  color="primary"
                  type="button"
                  [disabled]="updateLoading()"
                  (click)="onStart(sprint.id)">
                  <span i18n="Button that starts the sprint">Start Sprint</span>
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
                  <span i18n="Button that completes the sprint">
                    Complete Sprint
                  </span>
                </button>
              }
            </div>
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

  protected readonly assistant = inject(AiAssistantService);
  protected readonly sprintIcon = LucideCalendarClock;

  readonly sprintStatus = SprintStatus;
  readonly sprintId = signal<number | null>(null);
  private readonly sprintCommands = inject(SprintCommandsService);
  private readonly sprintResourceRef = sprintDetailResource(
    computed(() => this.sprintId() ?? undefined)
  );

  readonly sprint = this.sprintResourceRef.value;
  readonly loading = this.sprintResourceRef.isLoading;
  readonly loadError = computed(
    () => this.sprintResourceRef.error() as HttpErrorResponse | undefined
  );
  readonly updateLoading = this.sprintCommands.isUpdating;
  readonly canUpdate = hasPermission(PERMISSONS.sprints.update);
  readonly canManageTasks = hasPermission(PERMISSONS.sprints.manageTasks);

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
      width: '520px',
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
