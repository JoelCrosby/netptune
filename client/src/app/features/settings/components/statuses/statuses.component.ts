import { Component, computed, inject, signal } from '@angular/core';
import { EntityType } from '@core/models/entity-type';
import {
  Status,
  StatusCategory,
  statusCategoryLabels,
} from '@core/models/status';
import { StatusesService } from '@core/services/statuses.service';
import { DialogService } from '@core/services/dialog.service';
import {
  CreateStatusDialogComponent,
  CreateStatusDialogResult,
} from '@entry/dialogs/create-status-dialog/create-status-dialog.component';
import {
  EditStatusDialogComponent,
  EditStatusDialogResult,
} from '@entry/dialogs/edit-status-dialog/edit-status-dialog.component';
import {
  LucideArrowDown,
  LucideArrowUp,
  LucidePencil,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  TableComponent,
  TableEmptyCellDirective,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { finalize, first } from 'rxjs';

@Component({
  selector: 'app-statuses',
  imports: [
    StrokedButtonComponent,
    IconButtonComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
    TooltipDirective,
    LucideArrowDown,
    LucideArrowUp,
    LucidePencil,
    LucidePlus,
    LucideTrash2,
  ],
  template: `<section>
    <div class="mb-4 flex items-center justify-between gap-3">
      <h3 class="font-overpass text-[1.4rem] font-normal">Task statuses</h3>
      <button
        app-stroked-button
        type="button"
        [disabled]="loading()"
        (click)="openCreateDialog()">
        <svg lucidePlus class="h-4 w-4"></svg>
        <span>Create status</span>
      </button>
    </div>

    @if (error()) {
      <div class="text-danger mb-3 text-sm">{{ error() }}</div>
    }

    <app-table tableClass="min-w-[720px] table-fixed">
      <thead appTableHead>
        <tr appTableHeaderRow>
          <th class="w-16 px-4 py-3">Color</th>
          <th class="px-4 py-3">Name</th>
          <th class="w-44 px-4 py-3">Category</th>
          <th class="w-28 px-4 py-3">Order</th>
          <th class="w-28 px-4 py-3">Actions</th>
        </tr>
      </thead>
      <tbody>
        @for (status of orderedStatuses(); track status.id; let i = $index) {
          <tr appTableRow class="bg-card">
            <td class="px-4 py-2 align-middle">
              <span
                class="border-border block h-6 w-6 rounded-sm border"
                [style.background-color]="status.color ?? '#64748b'">
              </span>
            </td>
            <td class="px-4 py-2 align-middle">
              <button
                type="button"
                class="block w-full cursor-pointer truncate text-left font-medium"
                (click)="openEditDialog(status)">
                {{ status.name }}
              </button>
            </td>
            <td class="px-4 py-2 align-middle">
              {{ categoryLabel(status.category) }}
            </td>
            <td class="px-4 py-2 align-middle">
              <div class="flex gap-1">
                <button
                  app-icon-button
                  appTooltip="Move up"
                  aria-label="Move status up"
                  [disabled]="i === 0 || loading()"
                  (click)="move(status.id, -1)">
                  <svg lucideArrowUp class="h-4 w-4"></svg>
                </button>
                <button
                  app-icon-button
                  appTooltip="Move down"
                  aria-label="Move status down"
                  [disabled]="i === orderedStatuses().length - 1 || loading()"
                  (click)="move(status.id, 1)">
                  <svg lucideArrowDown class="h-4 w-4"></svg>
                </button>
              </div>
            </td>
            <td class="px-4 py-2 align-middle">
              <div class="flex gap-1">
                <button
                  app-icon-button
                  appTooltip="Edit"
                  aria-label="Edit status"
                  [disabled]="loading()"
                  (click)="openEditDialog(status)">
                  <svg lucidePencil class="h-4 w-4"></svg>
                </button>
                <button
                  app-icon-button
                  appTooltip="Delete"
                  aria-label="Delete status"
                  [disabled]="status.isSystem || loading()"
                  (click)="delete(status)">
                  <svg lucideTrash2 class="h-4 w-4"></svg>
                </button>
              </div>
            </td>
          </tr>
        } @empty {
          <tr>
            <td appTableEmptyCell colspan="5">No statuses</td>
          </tr>
        }
      </tbody>
    </app-table>
  </section>`,
})
export class StatusesComponent {
  private readonly statusesService = inject(StatusesService);
  private readonly dialog = inject(DialogService);

  readonly statuses = signal<Status[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly orderedStatuses = computed(() =>
    [...this.statuses()].sort(
      (a, b) => a.sortOrder - b.sortOrder || a.id - b.id
    )
  );

  constructor() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set(null);

    this.statusesService
      .get(EntityType.task)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (statuses) => {
          this.statuses.set(statuses);
        },
        error: () => this.error.set('Statuses could not be loaded.'),
      });
  }

  openCreateDialog() {
    const dialogRef = this.dialog.open<CreateStatusDialogResult>(
      CreateStatusDialogComponent,
      {
        width: '420px',
      }
    );

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        if (!result) return;

        this.create(result);
      },
    });
  }

  create(result: CreateStatusDialogResult) {
    const name = result.name.trim();
    if (!name) return;

    this.loading.set(true);
    this.error.set(null);

    this.statusesService
      .create({
        entityType: EntityType.task,
        name,
        category: result.category,
        color: '#64748b',
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess || !response.payload) {
            this.error.set(response.message ?? 'Status could not be created.');
            return;
          }

          this.load();
        },
        error: () => this.error.set('Status could not be created.'),
      });
  }

  openEditDialog(status: Status) {
    const dialogRef = this.dialog.open<EditStatusDialogResult, Status>(
      EditStatusDialogComponent,
      {
        data: status,
        width: '420px',
      }
    );

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        if (!result) return;

        this.update(status, result);
      },
    });
  }

  update(status: Status, result: EditStatusDialogResult) {
    const name = result.name.trim();
    if (!name) return;

    this.loading.set(true);
    this.error.set(null);

    this.statusesService
      .update({
        id: status.id,
        entityType: status.entityType,
        name,
        description: status.description?.trim() || null,
        color: result.color,
        category: result.category,
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(response.message ?? 'Status could not be saved.');
            return;
          }

          this.load();
        },
        error: () => this.error.set('Status could not be saved.'),
      });
  }

  delete(status: Status) {
    if (status.isSystem) return;

    this.loading.set(true);
    this.error.set(null);

    this.statusesService
      .delete(status.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(response.message ?? 'Status could not be deleted.');
            return;
          }

          this.load();
        },
        error: () => this.error.set('Status could not be deleted.'),
      });
  }

  move(statusId: number, direction: -1 | 1) {
    const ordered = this.orderedStatuses();
    const currentIndex = ordered.findIndex((status) => status.id === statusId);
    const nextIndex = currentIndex + direction;

    if (currentIndex < 0 || nextIndex < 0 || nextIndex >= ordered.length) {
      return;
    }

    const next = [...ordered];
    [next[currentIndex], next[nextIndex]] = [
      next[nextIndex],
      next[currentIndex],
    ];

    this.statuses.set(
      next.map((status, index) => ({ ...status, sortOrder: index }))
    );
    this.loading.set(true);
    this.error.set(null);

    this.statusesService
      .reorder({
        entityType: EntityType.task,
        statusIds: next.map((status) => status.id),
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(
              response.message ?? 'Statuses could not be reordered.'
            );
            this.load();
          }
        },
        error: () => {
          this.error.set('Statuses could not be reordered.');
          this.load();
        },
      });
  }

  categoryLabel(category: StatusCategory) {
    return statusCategoryLabels[category];
  }
}
