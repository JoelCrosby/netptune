import {
  Component,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Params, RouterLink } from '@angular/router';
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
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { debounceTime } from 'rxjs/operators';
import { finalize, first } from 'rxjs';

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
    PageContainerComponent,
    PageHeaderComponent,
    RouterLink,
    SearchInputComponent,
    TooltipDirective,
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
          i18n-title="Shown when a change to a relation type could not be saved"
          title="That change could not be saved"
          [description]="error() ?? ''"
          (retry)="reload()" />
      }

      <div class="mb-3 flex flex-row items-center gap-2">
        <app-search-input
          [term]="searchInput()"
          (searchChange)="searchInput.set($event ?? '')" />
      </div>

      <app-datatable
        tableClass="min-w-[820px] table-fixed"
        i18n-errorMessage="Shown when the relation type list fails to load"
        errorMessage="Relation types could not be loaded."
        i18n-itemLabel="Plural noun for relation types, used in the row summary"
        itemLabel="relations"
        [data]="data"
        [(sort)]="sort">
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
              [appTooltip]="moveTooltip(moveUpLabel)"
              i18n-aria-label="
                Accessible label for the button that moves a relation type up
              "
              aria-label="Move relation type up"
              [disabled]="!canMoveUp(i)"
              (click)="move(relationType.id, SortMoveDirection.up)">
              <svg lucideArrowUp class="h-4 w-4"></svg>
            </button>
            <button
              app-icon-button
              [appTooltip]="moveTooltip(moveDownLabel)"
              i18n-aria-label="
                Accessible label for the button that moves a relation type down
              "
              aria-label="Move relation type down"
              [disabled]="!canMoveDown(i)"
              (click)="move(relationType.id, SortMoveDirection.down)">
              <svg lucideArrowDown class="h-4 w-4"></svg>
            </button>
          </div>
        </ng-template>

        <ng-template appDatatableEmpty>
          @if (search()) {
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
    </app-page-container>
  `,
})
export class RelationTypesViewComponent {
  private readonly relationTypesService = inject(RelationTypesService);
  private readonly dialog = inject(DialogService);

  readonly SortMoveDirection = SortMoveDirection;
  readonly moveUpLabel = $localize`:Tooltip on the button that moves a row up:Move up`;
  readonly moveDownLabel = $localize`:Tooltip on the button that moves a row down:Move down`;
  readonly manualOrderOnlyLabel = $localize`:Explains that manual reordering needs the default, unfiltered view:Clear the search and sort by Order to reorder`;

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchInput = signal('');
  readonly sort = signal<DatatableSort | null>(null);
  private readonly reloadToken = signal(0);

  readonly search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  private readonly datatable = viewChild(DatatableComponent<RelationType>);

  // Rows only sit next to their sort-order neighbours in the default, unfiltered view,
  // so that is the only view where moving a row up or down means anything.
  readonly manualOrderActive = computed(() => {
    const sort = this.sort();
    const sortedByOrder = sort === null || sort.sortBy === 'sortOrder';

    return sortedByOrder && !this.search().trim();
  });

  private readonly resourceParams = computed<Params>(() => {
    const search = this.search().trim();

    return search ? { search } : {};
  });

  private readonly menu: DatatableMenuItem<RelationType>[] = [
    {
      label: $localize`:Row action that edits a relation type:Edit`,
      icon: LucideSettings2,
      onClick: (relationType) => this.openEditDialog(relationType),
      disabled: () => this.saving(),
    },
    {
      label: $localize`:Row action that deletes a relation type:Delete`,
      icon: LucideTrash2,
      onClick: (relationType) => this.delete(relationType),
      disabled: (relationType) => relationType.isSystem || this.saving(),
    },
  ];

  readonly data: DatatableDataSource<RelationType> = {
    key: 'workspace-relation-types',
    columns: [
      {
        id: 'color',
        header: $localize`:Column heading for the colour swatch:Color`,
        widthClass: 'w-16',
      },
      {
        id: 'name',
        header: $localize`:Column heading for the name:Name`,
        accessor: 'name',
        sortable: true,
      },
      {
        id: 'inverseName',
        header: $localize`:Column heading for the reverse direction of a relation:Inverse`,
        sortable: true,
        cellClass: 'text-muted truncate',
      },
      {
        id: 'category',
        header: $localize`:Column heading for the relation category:Category`,
        accessor: (relationType) => this.categoryLabel(relationType.category),
        sortable: true,
        widthClass: 'w-36',
      },
      {
        id: 'relationCount',
        header: $localize`:Column heading for the number of task links using a row:Relations`,
        accessor: 'relationCount',
        sortable: true,
        widthClass: 'w-28',
        cellClass: 'text-muted',
      },
      {
        id: 'sortOrder',
        header: $localize`:Column heading for the sort order:Order`,
        sortable: true,
        widthClass: 'w-28',
      },
    ],
    resource: { url: 'api/relation-types/page', params: this.resourceParams },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, relationType: RelationType) => relationType.id,
    menu: this.menu,
    reloadSignal: this.reloadToken,
  };

  constructor() {
    let previousSearch = this.search();

    effect(() => {
      const search = this.search();

      if (search === previousSearch) return;

      previousSearch = search;
      this.datatable()?.goToPage(1);
    });
  }

  reload() {
    this.error.set(null);
    this.reloadToken.update((token) => token + 1);
  }

  moveTooltip(label: string) {
    return this.manualOrderActive() ? label : this.manualOrderOnlyLabel;
  }

  canMoveUp(rowIndex: number) {
    const isFirstOverall = this.globalIndex(rowIndex) === 0;

    return this.manualOrderActive() && !this.saving() && !isFirstOverall;
  }

  canMoveDown(rowIndex: number) {
    const table = this.datatable();
    const isLastOverall =
      this.globalIndex(rowIndex) === (table?.totalCount() ?? 0) - 1;

    return this.manualOrderActive() && !this.saving() && !isLastOverall;
  }

  private globalIndex(rowIndex: number) {
    const table = this.datatable();

    if (!table) return rowIndex;

    return (table.currentPage() - 1) * table.pageSize() + rowIndex;
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

    this.saving.set(true);
    this.error.set(null);

    this.relationTypesService
      .create({
        name,
        inverseName: result.inverseName,
        category: result.category,
        color: fallbackColor,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess || !response.payload) {
            this.error.set(
              response.message ?? 'Relation type could not be created.'
            );
            return;
          }

          this.reload();
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

    this.saving.set(true);
    this.error.set(null);

    this.relationTypesService
      .update({
        id: relationType.id,
        name,
        inverseName: result.inverseName,
        description: relationType.description?.trim() || null,
        color: result.color,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(
              response.message ?? 'Relation type could not be saved.'
            );
            return;
          }

          this.reload();
        },
        error: () => this.error.set('Relation type could not be saved.'),
      });
  }

  delete(relationType: RelationType) {
    if (relationType.isSystem) return;

    this.saving.set(true);
    this.error.set(null);

    this.relationTypesService
      .delete(relationType.id)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(
              response.message ?? 'Relation type could not be deleted.'
            );
            return;
          }

          this.reload();
        },
        error: () => this.error.set('Relation type could not be deleted.'),
      });
  }

  move(relationTypeId: number, direction: SortMoveDirection) {
    this.saving.set(true);
    this.error.set(null);

    this.relationTypesService
      .move({ id: relationTypeId, direction })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(
              response.message ?? 'Relation types could not be reordered.'
            );
            return;
          }

          this.reload();
        },
        error: () => this.error.set('Relation types could not be reordered.'),
      });
  }

  isSymmetric(relationType: RelationType) {
    return isSymmetricCategory(relationType.category);
  }

  categoryLabel(category: RelationCategory) {
    return relationCategoryLabels[category];
  }
}
