import { DatePipe } from '@angular/common';
import {
  Component,
  computed,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { DatatableCellTemplateDirective } from '@app/static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@app/static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@app/static/components/datatable/datatable.component';
import { DatatableDataSource } from '@app/static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@app/static/components/empty-state/empty-state.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { TaskArchiveService } from '@core/store/tasks/task-archive.service';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { LucideArchiveRestore } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-archive-list',
  imports: [
    AvatarComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    DatePipe,
    LucideArchiveRestore,
    StrokedButtonComponent,
    TooltipDirective,
  ],
  template: `
    <div class="mb-4 flex h-10 items-center">
      @if (selectedCount() > 0) {
        <div class="ml-auto flex flex-row items-center gap-4">
          <span class="text-muted px-2 text-sm">
            {{ selectedCount() }} selected
          </span>
          <button app-stroked-button type="button" (click)="restoreSelected()">
            <svg lucideArchiveRestore class="h-4 w-4"></svg>
            <span>Restore</span>
          </button>
        </div>
      }
    </div>

    <app-datatable
      #datatable
      containerClass="h-[calc(100vh-312px)] min-h-160 overflow-auto"
      tableClass="min-w-[760px] table-fixed"
      rowClass="bg-card"
      [data]="taskData"
      [selection]="true"
      [customizableColumns]="true"
      [stickyHeader]="true"
      (selectionChanged)="selection.set($event)"
      (loaded)="onLoaded($event)">
      <ng-template appDatatableCell="systemId" let-task>
        <span class="bg-foreground/10 inline rounded px-1.5 py-0.5 text-sm">
          {{ task.systemId }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="name" let-task>
        <span class="block truncate font-medium">{{ task.name }}</span>
      </ng-template>

      <ng-template appDatatableCell="deletedBy" let-task>
        @if (task.deletedByUsername) {
          <div class="flex items-center gap-2">
            <app-avatar
              size="sm"
              [name]="task.deletedByUsername"
              [imageUrl]="task.deletedByPictureUrl" />
            <span class="truncate text-sm">{{ task.deletedByUsername }}</span>
          </div>
        } @else {
          <span class="text-muted text-sm">Unknown</span>
        }
      </ng-template>

      <ng-template appDatatableCell="updatedAt" let-task>
        <span
          class="text-muted text-sm"
          [appTooltip]="task.updatedAt | date: 'medium'">
          {{ task.updatedAt | date }}
        </span>
      </ng-template>

      <app-empty-state
        appDatatableEmpty
        title="There are currently no deleted tasks."
        description="Deleted tasks show up here, where they can be restored.">
        <svg emptyStateIcon size="38" lucideArchiveRestore></svg>
      </app-empty-state>
    </app-datatable>
  `,
})
export class ArchiveListComponent {
  private store = inject(Store);
  private archiveService = inject(TaskArchiveService);

  private datatable = viewChild(DatatableComponent<TaskViewModel>);
  private workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  readonly countChange = output<number>();

  readonly selection = signal<TaskViewModel[]>([]);
  readonly selectedCount = computed(() => this.selection().length);

  private readonly reloadVersion = signal(0);
  private readonly requestParams = signal({});

  taskData: DatatableDataSource<TaskViewModel> = {
    key: 'task-archive',
    columns: [
      {
        id: 'systemId',
        header: 'Key',
        accessor: 'systemId',
        sortable: true,
        widthClass: 'w-28',
      },
      {
        id: 'name',
        header: 'Task',
        accessor: 'name',
        sortable: true,
        cellClass: 'min-w-64',
      },
      {
        id: 'projectName',
        header: 'Project',
        accessor: 'projectName',
        sortKey: 'projectName',
        widthClass: 'w-48',
      },
      {
        id: 'deletedBy',
        header: 'Deleted by',
        widthClass: 'w-48',
      },
      {
        id: 'updatedAt',
        header: 'Deleted',
        sortKey: 'updatedAt',
        widthClass: 'w-40',
      },
    ],
    resource: {
      url: 'api/tasks/archive',
      params: this.requestParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
    menu: [
      {
        label: 'Restore',
        icon: LucideArchiveRestore,
        onClick: (task) => this.restore([task.id]),
      },
    ],
    reloadSignal: this.reloadVersion,
  };

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    if (event.hasValue) {
      this.countChange.emit(event.totalCount);
    }
  }

  restoreSelected() {
    this.restore(this.selection().map((task) => task.id));
  }

  private restore(ids: number[]) {
    const workspaceId = this.workspaceId();

    if (!workspaceId || ids.length === 0) return;

    this.archiveService
      .restore(`[workspace] ${workspaceId}`, ids)
      .subscribe(() => {
        this.datatable()?.clearSelection();
        this.selection.set([]);
        this.reloadVersion.update((version) => version + 1);
      });
  }
}
