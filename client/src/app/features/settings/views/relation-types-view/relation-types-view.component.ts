import { Component, computed, inject, signal } from '@angular/core';
import {
  RelationCategory,
  RelationType,
  isSymmetricCategory,
  relationCategoryLabels,
} from '@core/models/relation-type';
import { DialogService } from '@core/services/dialog.service';
import { RelationTypesService } from '@core/services/relation-types.service';
import {
  CreateRelationTypeDialogComponent,
  CreateRelationTypeDialogResult,
} from '@entry/dialogs/create-relation-type-dialog/create-relation-type-dialog.component';
import {
  EditRelationTypeDialogComponent,
  EditRelationTypeDialogResult,
} from '@entry/dialogs/edit-relation-type-dialog/edit-relation-type-dialog.component';
import {
  LucideArrowDown,
  LucideArrowUp,
  LucidePencil,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
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
  selector: 'app-relation-types-view',
  imports: [
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
    LucidePencil,
    LucidePlus,
    LucideTrash2,
  ],
  template: `<section>
    <app-section-header
      heading="Task relations"
      description='How tasks can be linked to one another. A relation reads one way from the source task and the other way from the target — "Blocks" one way, "Is Blocked By" the other.'>
      <button
        sectionHeaderActions
        app-stroked-button
        type="button"
        [disabled]="loading()"
        (click)="openCreateDialog()">
        <svg lucidePlus class="h-4 w-4"></svg>
        <span>Create relation type</span>
      </button>
    </app-section-header>

    @if (error()) {
      <div class="text-danger mb-3 text-sm">{{ error() }}</div>
    }

    <app-table tableClass="min-w-[820px] table-fixed">
      <thead appTableHead>
        <tr appTableHeaderRow>
          <th class="w-16 px-4 py-3">Color</th>
          <th class="px-4 py-3">Name</th>
          <th class="px-4 py-3">Inverse</th>
          <th class="w-36 px-4 py-3">Category</th>
          <th class="w-28 px-4 py-3">Order</th>
          <th class="w-28 px-4 py-3">Actions</th>
        </tr>
      </thead>
      <tbody>
        @for (
          relationType of orderedRelationTypes();
          track relationType.id;
          let i = $index
        ) {
          <tr appTableRow class="bg-card">
            <td class="px-4 py-2 align-middle">
              <app-color-swatch variant="swatch" [color]="relationType.color" />
            </td>
            <td class="px-4 py-2 align-middle">
              <button
                type="button"
                class="block w-full cursor-pointer truncate text-left font-medium"
                (click)="openEditDialog(relationType)">
                {{ relationType.name }}
              </button>
            </td>
            <td class="text-muted truncate px-4 py-2 align-middle">
              @if (isSymmetric(relationType)) {
                <span class="italic">Same both ways</span>
              } @else {
                {{ relationType.inverseName }}
              }
            </td>
            <td class="px-4 py-2 align-middle">
              {{ categoryLabel(relationType.category) }}
            </td>
            <td class="px-4 py-2 align-middle">
              <div class="flex gap-1">
                <button
                  app-icon-button
                  appTooltip="Move up"
                  aria-label="Move relation type up"
                  [disabled]="i === 0 || loading()"
                  (click)="move(relationType.id, -1)">
                  <svg lucideArrowUp class="h-4 w-4"></svg>
                </button>
                <button
                  app-icon-button
                  appTooltip="Move down"
                  aria-label="Move relation type down"
                  [disabled]="
                    i === orderedRelationTypes().length - 1 || loading()
                  "
                  (click)="move(relationType.id, 1)">
                  <svg lucideArrowDown class="h-4 w-4"></svg>
                </button>
              </div>
            </td>
            <td class="px-4 py-2 align-middle">
              <div class="flex gap-1">
                <button
                  app-icon-button
                  appTooltip="Edit"
                  aria-label="Edit relation type"
                  [disabled]="loading()"
                  (click)="openEditDialog(relationType)">
                  <svg lucidePencil class="h-4 w-4"></svg>
                </button>
                <button
                  app-icon-button
                  [appTooltip]="
                    relationType.isSystem
                      ? 'Built-in relation types cannot be deleted'
                      : 'Delete'
                  "
                  aria-label="Delete relation type"
                  [disabled]="relationType.isSystem || loading()"
                  (click)="delete(relationType)">
                  <svg lucideTrash2 class="h-4 w-4"></svg>
                </button>
              </div>
            </td>
          </tr>
        } @empty {
          <tr>
            <td appTableEmptyCell colspan="6">No relation types</td>
          </tr>
        }
      </tbody>
    </app-table>
  </section>`,
})
export class RelationTypesViewComponent {
  private readonly relationTypesService = inject(RelationTypesService);
  private readonly dialog = inject(DialogService);

  readonly relationTypes = signal<RelationType[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly orderedRelationTypes = computed(() =>
    [...this.relationTypes()].sort(
      (a, b) => a.sortOrder - b.sortOrder || a.id - b.id
    )
  );

  constructor() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set(null);

    this.relationTypesService
      .get()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (relationTypes) => {
          this.relationTypes.set(relationTypes);
        },
        error: () => this.error.set('Relation types could not be loaded.'),
      });
  }

  openCreateDialog() {
    const dialogRef = this.dialog.open<CreateRelationTypeDialogResult>(
      CreateRelationTypeDialogComponent,
      {
        width: '480px',
      }
    );

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        if (!result) return;

        this.create(result);
      },
    });
  }

  create(result: CreateRelationTypeDialogResult) {
    const name = result.name.trim();
    if (!name) return;

    this.loading.set(true);
    this.error.set(null);

    this.relationTypesService
      .create({
        name,
        inverseName: result.inverseName,
        category: result.category,
        color: '#64748b',
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess || !response.payload) {
            this.error.set(
              response.message ?? 'Relation type could not be created.'
            );
            return;
          }

          this.load();
        },
        error: () => this.error.set('Relation type could not be created.'),
      });
  }

  openEditDialog(relationType: RelationType) {
    const dialogRef = this.dialog.open<
      EditRelationTypeDialogResult,
      RelationType
    >(EditRelationTypeDialogComponent, {
      data: relationType,
      width: '480px',
    });

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        if (!result) return;

        this.update(relationType, result);
      },
    });
  }

  update(relationType: RelationType, result: EditRelationTypeDialogResult) {
    const name = result.name.trim();
    if (!name) return;

    this.loading.set(true);
    this.error.set(null);

    this.relationTypesService
      .update({
        id: relationType.id,
        name,
        inverseName: result.inverseName,
        description: relationType.description?.trim() || null,
        color: result.color,
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(
              response.message ?? 'Relation type could not be saved.'
            );
            return;
          }

          this.load();
        },
        error: () => this.error.set('Relation type could not be saved.'),
      });
  }

  delete(relationType: RelationType) {
    if (relationType.isSystem) return;

    this.loading.set(true);
    this.error.set(null);

    this.relationTypesService
      .delete(relationType.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(
              response.message ?? 'Relation type could not be deleted.'
            );
            return;
          }

          this.load();
        },
        error: () => this.error.set('Relation type could not be deleted.'),
      });
  }

  move(relationTypeId: number, direction: -1 | 1) {
    const ordered = this.orderedRelationTypes();
    const currentIndex = ordered.findIndex(
      (relationType) => relationType.id === relationTypeId
    );
    const nextIndex = currentIndex + direction;

    if (currentIndex < 0 || nextIndex < 0 || nextIndex >= ordered.length) {
      return;
    }

    const next = [...ordered];
    [next[currentIndex], next[nextIndex]] = [
      next[nextIndex],
      next[currentIndex],
    ];

    this.relationTypes.set(
      next.map((relationType, index) => ({ ...relationType, sortOrder: index }))
    );
    this.loading.set(true);
    this.error.set(null);

    this.relationTypesService
      .reorder({
        relationTypeIds: next.map((relationType) => relationType.id),
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(
              response.message ?? 'Relation types could not be reordered.'
            );
            this.load();
          }
        },
        error: () => {
          this.error.set('Relation types could not be reordered.');
          this.load();
        },
      });
  }

  isSymmetric(relationType: RelationType) {
    return isSymmetricCategory(relationType.category);
  }

  categoryLabel(category: RelationCategory) {
    return relationCategoryLabels[category];
  }
}
