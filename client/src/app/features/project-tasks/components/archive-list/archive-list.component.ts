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
import {
  DatatableColumn,
  DatatableMenuItem,
} from '@app/static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@app/static/components/empty-state/empty-state.component';
import { TaskTableComponent } from '@app/static/components/task-table.component';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { TaskArchiveService } from '@core/services/task-archive.service';
import { taskColumns } from '@core/tasks/task-columns';
import { LucideArchiveRestore } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-archive-list',
  imports: [
    AvatarComponent,
    DatatableCellTemplateDirective,
    DatatableEmptyDirective,
    EmptyStateComponent,
    LucideArchiveRestore,
    StrokedButtonComponent,
    TaskTableComponent,
  ],
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  template: `
    <div class="mb-4 flex h-10 shrink-0 items-center">
      @if (selectedCount() > 0) {
        <div class="ml-auto flex flex-row items-center gap-4">
          <span class="text-muted px-2 text-sm">
            <span
              i18n="
                Count of selected rows above a table. COUNT is the number
                selected
              ">
              {{
                selectedCount() // i18n(ph="COUNT")
              }}
              selected
            </span>
          </span>
          <button app-stroked-button type="button" (click)="restoreSelected()">
            <svg lucideArchiveRestore class="h-4 w-4"></svg>
            <span i18n="Button that restores the selected archived tasks">
              Restore
            </span>
          </button>
        </div>
      }
    </div>

    <app-task-table
      #table
      i18n-errorMessage="Shown when the archived task list fails to load"
      errorMessage="Archived tasks could not be loaded."
      key="task-archive"
      url="api/tasks/archive"
      tableClass="min-w-[760px] table-fixed"
      [fill]="true"
      [columns]="columns"
      [menu]="menu"
      [reloadSignal]="reloadVersion"
      [selection]="true"
      [customizableColumns]="true"
      [stickyHeader]="true"
      (selectionChanged)="selection.set($event)"
      (loaded)="onLoaded($event)">
      <ng-template appDatatableCell="deletedBy" let-task>
        @if (task.deletedByUsername) {
          <div class="flex items-center gap-2">
            <app-avatar
              size="sm"
              [name]="task.deletedByUsername"
              [imageUrl]="task.deletedByPictureUrl"
              [isServiceAccount]="task.deletedByIsServiceAccount ?? false" />
            <span class="truncate text-sm">{{ task.deletedByUsername }}</span>
          </div>
        } @else {
          <span
            class="text-muted text-sm"
            i18n="Shown in place of a value that is not known">
            Unknown
          </span>
        }
      </ng-template>

      <ng-template appDatatableEmpty>
        <app-empty-state
          i18n-title="Heading of the empty archive list"
          title="There are currently no deleted tasks."
          i18n-description="Explains what the archive list will contain"
          description="Deleted tasks show up here, where they can be restored.">
          <svg emptyStateIcon size="38" lucideArchiveRestore></svg>
        </app-empty-state>
      </ng-template>
    </app-task-table>
  `,
})
export class ArchiveListComponent {
  private archiveService = inject(TaskArchiveService);

  private table = viewChild(TaskTableComponent<TaskViewModel>);
  readonly countChange = output<number>();

  readonly selection = signal<TaskViewModel[]>([]);
  readonly selectedCount = computed(() => this.selection().length);

  readonly reloadVersion = signal(0);

  private readonly deletedByColumn: DatatableColumn<TaskViewModel> = {
    id: 'deletedBy',
    header: $localize`:Column heading for who deleted a task:Deleted by`,
    widthClass: 'w-48',
  };

  readonly columns: DatatableColumn<TaskViewModel>[] = [
    ...taskColumns<TaskViewModel>(['systemId', 'name', 'project'], {
      overrides: { project: { widthClass: 'w-48' } },
    }),
    this.deletedByColumn,
    ...taskColumns<TaskViewModel>(['updatedAt'], {
      overrides: {
        updatedAt: {
          header: $localize`:Column heading for when a task was deleted:Deleted`,
        },
      },
    }),
  ];

  readonly menu: DatatableMenuItem<TaskViewModel>[] = [
    {
      label: $localize`:Row action that restores an archived task:Restore`,
      icon: LucideArchiveRestore,
      onClick: (task) => this.restore([task.id]),
    },
  ];

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    if (event.hasValue) {
      this.countChange.emit(event.totalCount);
    }
  }

  restoreSelected() {
    this.restore(this.selection().map((task) => task.id));
  }

  private restore(ids: number[]) {
    if (ids.length === 0) return;

    this.archiveService.restore(ids).subscribe(() => {
      this.table()?.clearSelection();
      this.selection.set([]);
      this.reloadVersion.update((version) => version + 1);
    });
  }
}
