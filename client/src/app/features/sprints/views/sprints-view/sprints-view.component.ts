import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { Params, RouterLink } from '@angular/router';
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
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableColumn,
  DatatableDataSource,
  DatatableMenuItem,
} from '@static/components/datatable/datatable.types';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import {
  TabGroupComponent,
  TabItem,
} from '@static/components/tab-group/tab-group.component';
import { CreateSprintDialogComponent } from '../../dialogs/create-sprint-dialog.component';
import { EditSprintDialogComponent } from '../../dialogs/edit-sprint-dialog.component';
import { SprintStatusClassesPipe } from '../../pipes/sprint-status-classes.pipe';
import { SprintStatusLabelPipe } from '../../pipes/sprint-status-label.pipe';

type StatusFilter = SprintStatus | null;

@Component({
  imports: [
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    DatePipe,
    FlatButtonComponent,
    TabGroupComponent,
    LucidePlus,
    DatatableComponent,
    DatatableCellTemplateDirective,
    SprintStatusClassesPipe,
    SprintStatusLabelPipe,
  ],
  template: `
    <app-page-container [centerPage]="true">
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
        <app-page-loading />
      } @else {
        <div class="flex flex-col gap-6">
          <app-tab-group
            [tabs]="statusTabs()"
            [value]="selectedStatus()"
            (changed)="onStatusChanged($event)" />

          <app-datatable
            containerClass="h-[calc(100vh-314px)] min-h-160 overflow-auto"
            tableClass="min-w-[720px]"
            rowClass="bg-card"
            [data]="data()"
            [emptyMessage]="emptyMessage()">
            <ng-template appDatatableCell="name" let-sprint>
              <a class="font-medium hover:underline" [routerLink]="[sprint.id]">
                {{ sprint.name }}
              </a>
            </ng-template>

            <ng-template appDatatableCell="status" let-sprint>
              <div class="flex flex-wrap items-center gap-2">
                <span
                  class="rounded px-2 py-0.5 text-xs font-semibold"
                  [class]="sprint.status | sprintStatusClasses">
                  {{ sprint.status | sprintStatusLabel }}
                </span>
                @if (daysChip(sprint); as chip) {
                  <span
                    class="rounded px-2 py-0.5 text-xs font-medium"
                    [class]="chip.classes">
                    {{ chip.label }}
                  </span>
                }
              </div>
            </ng-template>

            <ng-template appDatatableCell="dates" let-sprint>
              <span class="text-muted text-sm whitespace-nowrap">
                {{ sprint.startDate | date: 'mediumDate' }} &ndash;
                {{ sprint.endDate | date: 'mediumDate' }}
              </span>
            </ng-template>

            <ng-template appDatatableCell="goal" let-sprint>
              @if (sprint.goal) {
                <span class="block max-w-xs truncate text-sm">
                  {{ sprint.goal }}
                </span>
              } @else {
                <span class="text-muted text-sm">&mdash;</span>
              }
            </ng-template>
          </app-datatable>
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

  readonly emptyMessage = computed(() => {
    const status = this.selectedStatus();
    return status === null
      ? 'No sprints yet.'
      : `No ${sprintStatusLabels[status].toLowerCase()} sprints.`;
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

  private readonly params = computed<Params>(() => {
    const status = this.selectedStatus();
    return status === null ? { take: 100 } : { statuses: [status], take: 100 };
  });

  private readonly columns: DatatableColumn<SprintViewModel>[] = [
    { id: 'name', header: 'Name', accessor: 'name', sortable: true },
    { id: 'status', header: 'Status', sortable: true, widthClass: 'w-52' },
    { id: 'dates', header: 'Dates', sortable: true, widthClass: 'w-56' },
    { id: 'goal', header: 'Goal' },
    {
      id: 'taskCount',
      header: 'Tasks',
      accessor: 'taskCount',
      sortable: true,
      align: 'end',
      widthClass: 'w-20',
    },
  ];

  private readonly menuItems: DatatableMenuItem<SprintViewModel>[] = [
    {
      label: 'Edit',
      icon: LucidePencil,
      onClick: (sprint) => this.onOpenEditDialog(sprint),
    },
    {
      label: 'Delete',
      icon: LucideTrash2,
      onClick: (sprint) => this.onDelete(sprint),
    },
  ];

  readonly data = computed<DatatableDataSource<SprintViewModel>>(() => ({
    key: 'sprints',
    columns: this.columns,
    resource: { url: 'api/sprints', params: this.params },
    rows: (response) =>
      Array.isArray(response) ? (response as SprintViewModel[]) : [],
    trackBy: (_: number, sprint: SprintViewModel) => sprint.id,
    menu: this.canUpdate() ? this.menuItems : undefined,
    reloadSignal: this.sprints,
  }));

  constructor() {
    dispatchForWorkspace(() => loadSprints.init({ filter: { take: 100 } }));
  }

  onStatusChanged(value: string | number | null) {
    this.selectedStatus.set(value as StatusFilter);
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
          this.store.dispatch(deleteSprint.init({ sprintId: sprint.id }));
        }
      });
  }
}
