import { LowerCasePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { CardListComponent } from '@app/static/components/card/card-list.component';
import { netptunePermissions } from '@core/auth/permissions';
import { SprintStatus, sprintStatusLabels } from '@core/enums/sprint-status';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { deleteSprint, loadSprints } from '@core/store/sprints/sprints.actions';
import {
  selectAllSprints,
  selectSprintsLoading,
} from '@core/store/sprints/sprints.selectors';
import { dispatchForWorkspace } from '@core/util/dispatch-for-workspace';
import { LucidePencil, LucidePlus, LucideTrash2 } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { CardComponent } from '@static/components/card/card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import {
  TabGroupComponent,
  TabItem,
} from '@static/components/tab-group/tab-group.component';
import { CreateSprintDialogComponent } from '../../dialogs/create-sprint-dialog.component';
import { EditSprintDialogComponent } from '../../dialogs/edit-sprint-dialog.component';
import { CardHeaderComponent } from '@app/static/components/card/card-header.component';

type StatusFilter = SprintStatus | null;

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LowerCasePipe,
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    FlatButtonComponent,
    IconButtonComponent,
    CardComponent,
    TabGroupComponent,
    LucidePlus,
    LucidePencil,
    LucideTrash2,
    CardListComponent,
    CardHeaderComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Sprints">
        <a app-flat-button [routerLink]="['backlog']">Backlog</a>
        @if (canCreate()) {
          <button
            app-flat-button
            color="primary"
            (click)="onOpenCreateDialog()">
            <svg lucidePlus class="h-4 w-4"></svg>
            New Sprint
          </button>
        }
      </app-page-header>

      @if (loading()) {
        <div class="flex h-full flex-col items-center justify-center">
          <app-spinner diameter="32px" />
        </div>
      } @else {
        <div class="flex flex-col gap-6">
          <app-tab-group
            [tabs]="statusTabs()"
            [value]="selectedStatus()"
            (changed)="onStatusChanged($event)" />

          <app-card-list>
            @for (sprint of filteredSprints(); track sprint.id) {
              <app-card>
                <app-card-header>
                  <div class="flex flex-wrap items-center gap-2">
                    <h2 class="text-xl font-semibold">{{ sprint.name }}</h2>
                    <span
                      class="rounded px-2 py-0.5 text-xs font-semibold"
                      [class]="statusClasses(sprint.status)">
                      {{ statusLabel(sprint.status) }}
                    </span>
                    @if (daysChip(sprint); as chip) {
                      <span
                        class="rounded px-2 py-0.5 text-xs font-medium"
                        [class]="chip.classes">
                        {{ chip.label }}
                      </span>
                    }
                  </div>

                  <div class="flex shrink-0 items-center gap-1">
                    @if (canUpdate()) {
                      <button
                        app-icon-button
                        type="button"
                        title="Edit sprint"
                        (click)="onOpenEditDialog(sprint)">
                        <svg lucidePencil class="h-4 w-4"></svg>
                      </button>
                    }
                    @if (canUpdate()) {
                      <button
                        app-icon-button
                        type="button"
                        title="Delete sprint"
                        (click)="onDelete(sprint)">
                        <svg lucideTrash2 class="h-4 w-4"></svg>
                      </button>
                    }
                  </div>
                </app-card-header>

                <div class="flex items-start justify-between gap-4"></div>

                @if (sprint.goal) {
                  <p class="text-sm">{{ sprint.goal }}</p>
                }

                <div class="flex items-center justify-between gap-4">
                  <span class="text-muted text-xs">
                    {{ sprint.taskCount }}
                    {{ sprint.taskCount === 1 ? 'task' : 'tasks' }}
                  </span>
                  <a app-flat-button color="primary" [routerLink]="[sprint.id]">
                    Open
                  </a>
                </div>
              </app-card>
            } @empty {
              <app-card class="text-center">
                @if (selectedStatus() !== null) {
                  No {{ statusLabel(selectedStatus()!) | lowercase }} sprints.
                } @else {
                  No sprints yet.
                }
              </app-card>
            }
          </app-card-list>
        </div>
      }
    </app-page-container>
  `,
})
export class SprintsViewComponent {
  private store = inject(Store);
  private dialog = inject(DialogService);
  private confirmation = inject(ConfirmationService);

  readonly loading = this.store.selectSignal(selectSprintsLoading);
  readonly sprints = this.store.selectSignal(selectAllSprints);
  readonly canCreate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.create)
  );
  readonly canUpdate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.update)
  );

  readonly selectedStatus = signal<StatusFilter>(SprintStatus.active);

  readonly filteredSprints = computed(() => {
    const status = this.selectedStatus();
    const sprints = this.sprints();
    return status === null
      ? sprints
      : sprints.filter((s) => s.status === status);
  });

  readonly statusTabs = computed((): TabItem[] => {
    const sprints = this.sprints();
    return [
      {
        label: 'Active',
        value: SprintStatus.active,
        badge: sprints.filter((s) => s.status === SprintStatus.active).length,
      },
      {
        label: 'Planning',
        value: SprintStatus.planning,
        badge: sprints.filter((s) => s.status === SprintStatus.planning).length,
      },
      {
        label: 'Completed',
        value: SprintStatus.completed,
        badge: sprints.filter((s) => s.status === SprintStatus.completed)
          .length,
      },
      {
        label: 'All',
        value: null,
        badge: sprints.length,
      },
    ];
  });

  constructor() {
    dispatchForWorkspace(() => loadSprints({ filter: { take: 100 } }));
  }

  onStatusChanged(value: string | number | null) {
    this.selectedStatus.set(value as StatusFilter);
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

  daysChip(sprint: SprintViewModel): { label: string; classes: string } | null {
    if (sprint.status !== SprintStatus.active) return null;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const end = new Date(sprint.endDate);
    end.setHours(0, 0, 0, 0);
    const diff = Math.ceil((end.getTime() - today.getTime()) / 86_400_000);

    if (diff < 0) {
      return {
        label: `${Math.abs(diff)}d overdue`,
        classes: 'bg-red-100 text-red-700',
      };
    }
    if (diff === 0) {
      return { label: 'Due today', classes: 'bg-orange-100 text-orange-700' };
    }
    if (diff <= 3) {
      return {
        label: `${diff}d left`,
        classes: 'bg-orange-100 text-orange-700',
      };
    }
    return {
      label: `${diff}d left`,
      classes: 'bg-neutral-100 text-neutral-600',
    };
  }

  onOpenCreateDialog() {
    this.dialog.open(CreateSprintDialogComponent, { width: '520px' });
  }

  onOpenEditDialog(sprint: SprintViewModel) {
    this.dialog.open(EditSprintDialogComponent, {
      width: '520px',
      data: sprint,
    });
  }

  onDelete(sprint: SprintViewModel) {
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
        }
      });
  }
}
