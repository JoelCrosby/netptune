import { Component, computed, inject, signal } from '@angular/core';
import { EntityType } from '@core/models/entity-type';
import {
  Status,
  StatusCategory,
  statusCategoryLabels,
} from '@core/models/status';
import { StatusesService } from '@core/services/statuses.service';
import { DialogService } from '@core/services/dialog.service';
import { fallbackColor } from '@core/util/colors/colors';
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
  LucideSettings2,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
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
  selector: 'app-statuses-view',
  imports: [
    ErrorStateComponent,
    StrokedButtonComponent,
    ColorSwatchComponent,
    SectionHeaderComponent,
    IconButtonComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
    TooltipDirective,
    LucideArrowDown,
    LucideArrowUp,
    LucideSettings2,
    LucidePlus,
    LucideTrash2,
  ],
  template: `
    <section>
      <app-section-header
        i18n-heading="Section heading for task statuses"
        heading="Task statuses">
        <button
          sectionHeaderActions
          app-stroked-button
          type="button"
          [disabled]="loading()"
          (click)="openCreateDialog()">
          <svg lucidePlus class="h-4 w-4"></svg>
          <span i18n="Button that opens the create-status dialog">
            Create status
          </span>
        </button>
      </app-section-header>

      @if (error()) {
        <app-error-state
          compact
          i18n-title="Shown when the status list fails to load"
          title="Statuses could not be loaded"
          [description]="error() ?? ''"
          (retry)="load()" />
      } @else {
        <app-table tableClass="min-w-[720px] table-fixed">
          <thead appTableHead>
            <tr appTableHeaderRow>
              <th class="w-16 px-4 py-3">
                <span i18n="Column heading for the colour swatch">Color</span>
              </th>
              <th class="px-4 py-3">
                <span i18n="Column heading for the name">Name</span>
              </th>
              <th class="w-44 px-4 py-3">
                <span i18n="Column heading for the status category">
                  Category
                </span>
              </th>
              <th class="w-28 px-4 py-3">
                <span i18n="Column heading for the sort order">Order</span>
              </th>
              <th class="w-28 px-4 py-3">
                <span i18n="Column heading for the row action buttons">
                  Actions
                </span>
              </th>
            </tr>
          </thead>
          <tbody>
            @for (
              status of orderedStatuses();
              track status.id;
              let i = $index
            ) {
              <tr appTableRow class="bg-card">
                <td class="px-4 py-2 align-middle">
                  <app-color-swatch variant="swatch" [color]="status.color" />
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
                      i18n-appTooltip="
                        Tooltip on the button that moves a row up
                      "
                      appTooltip="Move up"
                      i18n-aria-label="
                        Accessible label for the button that moves a status up
                      "
                      aria-label="Move status up"
                      [disabled]="i === 0 || loading()"
                      (click)="move(status.id, -1)">
                      <svg lucideArrowUp class="h-4 w-4"></svg>
                    </button>
                    <button
                      app-icon-button
                      i18n-appTooltip="
                        Tooltip on the button that moves a row down
                      "
                      appTooltip="Move down"
                      i18n-aria-label="
                        Accessible label for the button that moves a status down
                      "
                      aria-label="Move status down"
                      [disabled]="
                        i === orderedStatuses().length - 1 || loading()
                      "
                      (click)="move(status.id, 1)">
                      <svg lucideArrowDown class="h-4 w-4"></svg>
                    </button>
                  </div>
                </td>
                <td class="px-4 py-2 align-middle">
                  <div class="flex gap-1">
                    <button
                      app-icon-button
                      i18n-appTooltip="Tooltip on the button that edits a row"
                      appTooltip="Edit"
                      i18n-aria-label="
                        Accessible label for the button that edits a status
                      "
                      aria-label="Edit status"
                      [disabled]="loading()"
                      (click)="openEditDialog(status)">
                      <svg lucideSettings2 class="h-4 w-4"></svg>
                    </button>
                    <button
                      app-icon-button
                      i18n-appTooltip="Tooltip on the button that deletes a row"
                      appTooltip="Delete"
                      i18n-aria-label="
                        Accessible label for the button that deletes a status
                      "
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
                <td appTableEmptyCell colspan="5">
                  <span i18n="Empty state for the status list">
                    No statuses yet. Create one to describe your workflow.
                  </span>
                </td>
              </tr>
            }
          </tbody>
        </app-table>
      }
    </section>
  `,
})
export class StatusesViewComponent {
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
        color: fallbackColor,
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
