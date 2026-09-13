import { Component, inject, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  RelationCategory,
  RelationType,
  isSymmetricCategory,
  relationCategoryLabels,
} from '@core/models/relation-type';
import { SortMoveDirection } from '@core/models/sort-move-direction';
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
  LucideWaypoints,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableMenuItem,
} from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { orderableEntityList } from '../../shared/orderable-entity-list';

@Component({
  selector: 'app-relation-types-view',
  imports: [
    ColorSwatchComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    ErrorStateComponent,
    IconButtonComponent,
    LucideArrowDown,
    LucideArrowUp,
    LucideWaypoints,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    RouterLink,
    SearchInputComponent,
    TooltipDirective,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for workspace task relation types"
        title="Relations"
        i18n-actionTitle="Button that opens the create-relation-type dialog"
        actionTitle="Create relation type"
        i18n-filtersLabel="Accessible name of the relation type list filter row"
        filtersLabel="Filter relation types"
        [count]="table.loadedCount()"
        (actionClick)="openCreateDialog()">
        <div pageHeaderFilters class="flex flex-row items-center gap-2.5">
          <app-search-input
            [term]="list.searchInput()"
            (searchChange)="list.searchInput.set($event ?? '')" />
        </div>
      </app-page-header>

      <app-page-body>
        @if (list.error()) {
          <app-error-state
            compact
            class="mb-3 shrink-0"
            i18n-title="
              Shown when a change to a relation type could not be saved
            "
            title="That change could not be saved"
            [description]="list.error() ?? ''"
            (retry)="list.reload()" />
        }

        <app-datatable
          #table
          autoFill
          stickyHeader
          tableClass="md:min-w-[820px] table-fixed"
          i18n-errorMessage="Shown when the relation type list fails to load"
          errorMessage="Relation types could not be loaded."
          i18n-itemLabel="
            Plural noun for relation types, used in the row summary
          "
          itemLabel="relations"
          [data]="data"
          [(sort)]="list.sort">
          <ng-template appDatatableCell="color" let-relationType>
            <app-color-swatch variant="swatch" [color]="relationType.color" />
          </ng-template>

          <ng-template appDatatableCell="name" let-relationType>
            <a
              class="block w-full truncate text-left font-medium hover:underline"
              [routerLink]="[relationType.id]">
              {{ relationType.name }}
            </a>
          </ng-template>

          <ng-template appDatatableCell="inverseName" let-relationType>
            @if (isSymmetric(relationType)) {
              <span
                class="italic"
                i18n="Shown when a relation reads the same in both directions">
                Same both ways
              </span>
            } @else {
              {{ relationType.inverseName }}
            }
          </ng-template>

          <ng-template
            appDatatableCell="sortOrder"
            let-relationType
            let-i="rowIndex">
            <div class="flex gap-1">
              <button
                app-icon-button
                [appTooltip]="list.moveTooltip(list.moveUpLabel)"
                i18n-aria-label="
                  Accessible label for the button that moves a relation type up
                "
                aria-label="Move relation type up"
                [disabled]="!list.canMoveUp(i)"
                (click)="move(relationType.id, SortMoveDirection.up)">
                <svg lucideArrowUp class="h-4 w-4"></svg>
              </button>
              <button
                app-icon-button
                [appTooltip]="list.moveTooltip(list.moveDownLabel)"
                i18n-aria-label="
                  Accessible label for the button that moves a relation type
                  down
                "
                aria-label="Move relation type down"
                [disabled]="!list.canMoveDown(i)"
                (click)="move(relationType.id, SortMoveDirection.down)">
                <svg lucideArrowDown class="h-4 w-4"></svg>
              </button>
            </div>
          </ng-template>

          <ng-template appDatatableEmpty>
            @if (list.search()) {
              <app-empty-state
                compact
                i18n-title="Heading shown when a search matches nothing"
                title="No relation types match your search."
                i18n-description="Advice shown when a search matches nothing"
                description="Try a different term.">
                <svg emptyStateIcon size="38" lucideWaypoints></svg>
              </app-empty-state>
            } @else {
              <app-empty-state
                compact
                i18n-title="Heading of an empty relation type list"
                title="No relation types yet."
                i18n-description="Explains what relation types are for"
                description="Create one to link related tasks.">
                <svg emptyStateIcon size="38" lucideWaypoints></svg>
              </app-empty-state>
            }
          </ng-template>
        </app-datatable>
      </app-page-body>
    </app-page-container>
  `,
})
export class RelationTypesViewComponent {
  private readonly relationTypesService = inject(RelationTypesService);
  private readonly dialog = inject(DialogService);

  readonly SortMoveDirection = SortMoveDirection;

  private readonly datatable = viewChild(DatatableComponent<RelationType>);

  readonly list = orderableEntityList(this.datatable);

  private readonly menu: DatatableMenuItem<RelationType>[] = [
    {
      label: $localize`:Row action that edits a relation type:Edit`,
      icon: LucideSettings2,
      onClick: (relationType) => this.openEditDialog(relationType),
      disabled: () => this.list.pending(),
    },
    {
      label: $localize`:Row action that deletes a relation type:Delete`,
      icon: LucideTrash2,
      onClick: (relationType) => this.delete(relationType),
      disabled: (relationType) => relationType.isSystem || this.list.pending(),
    },
  ];

  readonly data: DatatableDataSource<RelationType> = {
    key: 'workspace-relation-types',
    columns: [
      {
        id: 'color',
        header: $localize`:Column heading for the colour swatch:Color`,
        visibleOnMobile: true,
        widthClass: 'w-16',
      },
      {
        id: 'name',
        header: $localize`:Column heading for the name:Name`,
        visibleOnMobile: true,
        accessor: 'name',
        sortable: true,
      },
      {
        id: 'inverseName',
        header: $localize`:Column heading for the reverse direction of a relation:Inverse`,
        visibleOnMobile: false,
        sortable: true,
        cellClass: 'text-muted truncate',
      },
      {
        id: 'category',
        header: $localize`:Column heading for the relation category:Category`,
        visibleOnMobile: false,
        accessor: (relationType) => this.categoryLabel(relationType.category),
        sortable: true,
        widthClass: 'w-36',
      },
      {
        id: 'relationCount',
        header: $localize`:Column heading for the number of task links using a row:Relations`,
        visibleOnMobile: true,
        accessor: 'relationCount',
        sortable: true,
        widthClass: 'w-28',
        cellClass: 'text-muted',
      },
      {
        id: 'sortOrder',
        header: $localize`:Column heading for the sort order:Order`,
        visibleOnMobile: false,
        sortable: true,
        widthClass: 'w-28',
      },
    ],
    resource: {
      url: 'api/relation-types/page',
      params: this.list.searchParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, relationType: RelationType) => relationType.id,
    menu: this.menu,
    reloadSignal: this.list.reloadToken,
  };

  async openCreateDialog() {
    const result =
      await this.dialog.openForResult<CreateRelationTypeDialogResult>(
        CreateRelationTypeDialogComponent,
        { width: '480px' }
      );

    if (!result) return;

    this.create(result);
  }

  create(result: CreateRelationTypeDialogResult) {
    const name = result.name.trim();
    if (!name) return;

    const request = this.relationTypesService.create({
      name,
      inverseName: result.inverseName,
      category: result.category,
      color: fallbackColor,
    });

    this.list.create(request, 'Relation type could not be created.');
  }

  async openEditDialog(relationType: RelationType) {
    const result = await this.dialog.openForResult<
      EditRelationTypeDialogResult,
      RelationType
    >(EditRelationTypeDialogComponent, {
      data: relationType,
      width: '480px',
    });

    if (!result) return;

    this.update(relationType, result);
  }

  update(relationType: RelationType, result: EditRelationTypeDialogResult) {
    const name = result.name.trim();
    if (!name) return;

    const request = this.relationTypesService.update({
      id: relationType.id,
      name,
      inverseName: result.inverseName,
      description: relationType.description?.trim() || null,
      color: result.color,
    });

    this.list.save(request, 'Relation type could not be saved.');
  }

  delete(relationType: RelationType) {
    if (relationType.isSystem) return;

    const request = this.relationTypesService.delete(relationType.id);

    this.list.save(request, 'Relation type could not be deleted.');
  }

  move(relationTypeId: number, direction: SortMoveDirection) {
    const request = this.relationTypesService.move({
      id: relationTypeId,
      direction,
    });

    this.list.save(request, 'Relation types could not be reordered.');
  }

  isSymmetric(relationType: RelationType) {
    return isSymmetricCategory(relationType.category);
  }

  categoryLabel(category: RelationCategory) {
    return relationCategoryLabels[category];
  }
}
