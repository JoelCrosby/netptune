import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  RelationCategory,
  RelationType,
  isSymmetricCategory,
  relationCategoryLabels,
} from '@core/models/relation-type';
import { DialogService } from '@core/services/dialog.service';
import { RelationTypesService } from '@core/services/relation-types.service';
import { fallbackColor } from '@core/util/colors/colors';
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
  LucideSettings2,
  LucideTrash2,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
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
    ErrorStateComponent,
    ColorSwatchComponent,
    PageContainerComponent,
    PageHeaderComponent,
    IconButtonComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
    TooltipDirective,
    RouterLink,
    LucideArrowDown,
    LucideArrowUp,
    LucideSettings2,
    LucideTrash2,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for workspace task relation types"
        title="Relations"
        i18n-actionTitle="Button that opens the create-relation-type dialog"
        actionTitle="Create relation type"
        (actionClick)="openCreateDialog()" />

      <p
        class="text-muted mb-4 max-w-3xl text-sm"
        i18n="
          Explains how relation direction works. The quoted example names are
          the default relation type and its inverse
        ">
        How tasks can be linked to one another. A relation reads one way from
        the source task and the other way from the target — "Blocks" one way,
        "Is Blocked By" the other.
      </p>

      @if (error()) {
        <app-error-state
          compact
          i18n-title="Shown when the relation type list fails to load"
          title="Relation types could not be loaded"
          [description]="error() ?? ''"
          (retry)="load()" />
      } @else {
        <app-table tableClass="min-w-[820px] table-fixed">
          <thead appTableHead>
            <tr appTableHeaderRow>
              <th class="w-16 px-4 py-3">
                <span i18n="Column heading for the colour swatch">Color</span>
              </th>
              <th class="px-4 py-3">
                <span i18n="Column heading for the name">Name</span>
              </th>
              <th class="px-4 py-3">
                <span
                  i18n="Column heading for the reverse direction of a relation">
                  Inverse
                </span>
              </th>
              <th class="w-36 px-4 py-3">
                <span i18n="Column heading for the relation category">
                  Category
                </span>
              </th>
              <th class="w-28 px-4 py-3">
                <span
                  i18n="
                    Column heading for the number of task links using a row
                  ">
                  Relations
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
              relationType of orderedRelationTypes();
              track relationType.id;
              let i = $index
            ) {
              <tr appTableRow class="bg-card">
                <td class="px-4 py-2 align-middle">
                  <app-color-swatch
                    variant="swatch"
                    [color]="relationType.color" />
                </td>
                <td class="px-4 py-2 align-middle">
                  <a
                    class="block w-full truncate text-left font-medium hover:underline"
                    [routerLink]="[relationType.id]">
                    {{ relationType.name }}
                  </a>
                </td>
                <td class="text-muted truncate px-4 py-2 align-middle">
                  @if (isSymmetric(relationType)) {
                    <span
                      class="italic"
                      i18n="
                        Shown when a relation reads the same in both directions
                      ">
                      Same both ways
                    </span>
                  } @else {
                    {{ relationType.inverseName }}
                  }
                </td>
                <td class="px-4 py-2 align-middle">
                  {{ categoryLabel(relationType.category) }}
                </td>
                <td class="text-muted px-4 py-2 align-middle">
                  {{ relationType.relationCount }}
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
                        Accessible label for the button that moves a relation
                        type up
                      "
                      aria-label="Move relation type up"
                      [disabled]="i === 0 || loading()"
                      (click)="move(relationType.id, -1)">
                      <svg lucideArrowUp class="h-4 w-4"></svg>
                    </button>
                    <button
                      app-icon-button
                      i18n-appTooltip="
                        Tooltip on the button that moves a row down
                      "
                      appTooltip="Move down"
                      i18n-aria-label="
                        Accessible label for the button that moves a relation
                        type down
                      "
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
                      i18n-appTooltip="Tooltip on the button that edits a row"
                      appTooltip="Edit"
                      i18n-aria-label="
                        Accessible label for the button that edits a relation
                        type
                      "
                      aria-label="Edit relation type"
                      [disabled]="loading()"
                      (click)="openEditDialog(relationType)">
                      <svg lucideSettings2 class="h-4 w-4"></svg>
                    </button>
                    <button
                      app-icon-button
                      [appTooltip]="
                        relationType.isSystem
                          ? 'Built-in relation types cannot be deleted'
                          : 'Delete'
                      "
                      i18n-aria-label="
                        Accessible label for the button that deletes a relation
                        type
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
                <td appTableEmptyCell colspan="7">
                  <span i18n="Empty state for the relation type list">
                    No relation types yet. Create one to link related tasks.
                  </span>
                </td>
              </tr>
            }
          </tbody>
        </app-table>
      }
    </app-page-container>
  `,
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
        color: fallbackColor,
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
