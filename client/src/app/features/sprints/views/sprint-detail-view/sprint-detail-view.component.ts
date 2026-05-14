import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { SprintStatus, sprintStatusLabels } from '@core/enums/sprint-status';
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
import { Store } from '@ngrx/store';
import { LucidePencil, LucideTrash2 } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { distinctUntilChanged, map } from 'rxjs/operators';
import { EditSprintDialogComponent } from '../../dialogs/edit-sprint-dialog.component';
import { SprintCompletionDialogComponent } from '../../dialogs/sprint-completion-dialog.component';
import { SprintStatsComponent } from '../../components/sprint-stats.component';
import { SprintTaskListComponent } from '../../components/sprint-task-list.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    FlatButtonComponent,
    IconButtonComponent,
    LucidePencil,
    LucideTrash2,
    SprintStatsComponent,
    SprintTaskListComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Sprint" />

      @if (loading()) {
        <div class="flex h-full flex-col items-center justify-center">
          <app-spinner diameter="32px" />
        </div>
      } @else if (sprint(); as sprint) {
        <section class="flex flex-col gap-4">
          <div class="flex flex-wrap items-start justify-between gap-4">
            <div class="min-w-0 flex-1">
              <div class="mb-1 flex flex-wrap items-center gap-2">
                <h1 class="text-2xl font-semibold">{{ sprint.name }}</h1>
                <span
                  class="rounded-sm px-2.5 py-0.5 text-xs font-semibold"
                  [class]="statusClasses(sprint.status)">
                  {{ statusLabel(sprint.status) }}
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

            <div class="flex shrink-0 items-center gap-1">
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

              @if (canUpdate() && sprint.status === sprintStatus.planning) {
                <button
                  app-flat-button
                  color="primary"
                  type="button"
                  class="ml-2"
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
                  class="ml-2"
                  [disabled]="updateLoading()"
                  (click)="onComplete(sprint)">
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
          this.store.dispatch(loadSprintDetail({ sprintId }));
        }
      });
  }

  statusLabel(status: SprintStatus) {
    return sprintStatusLabels[status];
  }

  statusClasses(status: SprintStatus): string {
    switch (status) {
      case SprintStatus.active:
        return 'bg-green-100 text-green-800';
      case SprintStatus.planning:
        return 'bg-blue-100 text-blue-800';
      case SprintStatus.completed:
        return 'bg-neutral-100 text-neutral-700';
      case SprintStatus.cancelled:
        return 'bg-red-100 text-red-700';
    }
  }

  daysChip(sprint: SprintDetailViewModel): { label: string; classes: string } | null {
    if (sprint.status !== SprintStatus.active) return null;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const end = new Date(sprint.endDate);
    end.setHours(0, 0, 0, 0);
    const diff = Math.ceil((end.getTime() - today.getTime()) / 86_400_000);

    if (diff < 0) {
      return { label: `${Math.abs(diff)}d overdue`, classes: 'bg-red-100 text-red-700' };
    }
    if (diff === 0) {
      return { label: 'Due today', classes: 'bg-orange-100 text-orange-700' };
    }
    if (diff <= 3) {
      return { label: `${diff}d left`, classes: 'bg-orange-100 text-orange-700' };
    }
    return { label: `${diff}d left`, classes: 'bg-neutral-100 text-neutral-600' };
  }

  onEdit(sprint: SprintDetailViewModel) {
    this.dialog.open(EditSprintDialogComponent, { width: '520px', data: sprint });
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
          this.store.dispatch(deleteSprint({ sprintId: sprint.id }));
          this.router.navigate(['../'], { relativeTo: this.route });
        }
      });
  }
}
