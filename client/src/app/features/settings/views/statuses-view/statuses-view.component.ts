import {
  Component,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { getErrorMessage } from '@core/util/error-message';
import { Params, RouterLink } from '@angular/router';
import { EntityType } from '@core/models/entity-type';
import { SortMoveDirection } from '@core/models/sort-move-direction';
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
  LucideCircleDashed,
  LucideSettings2,
  LucideTrash2,
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
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { debounceTime } from 'rxjs/operators';
import { finalize, first } from 'rxjs';

@Component({
  selector: 'app-statuses-view',
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
    LucideCircleDashed,
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
        i18n-title="Page title for workspace task statuses"
        title="Statuses"
        i18n-actionTitle="Button that opens the create-status dialog"
        actionTitle="Create status"
        i18n-filtersLabel="Accessible name of the status list filter row"
        filtersLabel="Filter statuses"
        [count]="count()"
        (actionClick)="openCreateDialog()">
        <div pageHeaderFilters class="flex flex-row items-center gap-2.5">
          <app-search-input
            [term]="searchInput()"
            (searchChange)="searchInput.set($event ?? '')" />
        </div>
      </app-page-header>

      <app-page-body>
        @if (error()) {
          <app-error-state
            compact
            class="mb-3 shrink-0"
            i18n-title="Shown when a change to a status could not be saved"
            title="That change could not be saved"
            [description]="error() ?? ''"
            (retry)="reload()" />
        }

        <app-datatable
          autoFill
          stickyHeader
          tableClass="min-w-[720px] table-fixed"
          i18n-errorMessage="Shown when the status list fails to load"
          errorMessage="Statuses could not be loaded."
          i18n-itemLabel="Plural noun for statuses, used in the row summary"
          itemLabel="statuses"
          [data]="data"
          [(sort)]="sort"
          (loaded)="count.set($event.hasValue ? $event.totalCount : null)">
          <ng-template appDatatableCell="color" let-status>
            <app-color-swatch variant="swatch" [color]="status.color" />
          </ng-template>

          <ng-template appDatatableCell="name" let-status>
            <a
              class="block w-full truncate text-left font-medium hover:underline"
              [routerLink]="[status.id]">
              {{ status.name }}
            </a>
          </ng-template>

          <ng-template appDatatableCell="sortOrder" let-status let-i="rowIndex">
            <div class="flex gap-1">
              <button
                app-icon-button
                [appTooltip]="moveTooltip(moveUpLabel)"
                i18n-aria-label="
                  Accessible label for the button that moves a status up
                "
                aria-label="Move status up"
                [disabled]="!canMoveUp(i)"
                (click)="move(status.id, SortMoveDirection.up)">
                <svg lucideArrowUp class="h-4 w-4"></svg>
              </button>
              <button
                app-icon-button
                [appTooltip]="moveTooltip(moveDownLabel)"
                i18n-aria-label="
                  Accessible label for the button that moves a status down
                "
                aria-label="Move status down"
                [disabled]="!canMoveDown(i)"
                (click)="move(status.id, SortMoveDirection.down)">
                <svg lucideArrowDown class="h-4 w-4"></svg>
              </button>
            </div>
          </ng-template>

          <ng-template appDatatableEmpty>
            @if (search()) {
              <app-empty-state
                compact
                i18n-title="Heading shown when a search matches nothing"
                title="No statuses match your search."
                i18n-description="Advice shown when a search matches nothing"
                description="Try a different term.">
                <svg emptyStateIcon size="38" lucideCircleDashed></svg>
              </app-empty-state>
            } @else {
              <app-empty-state
                compact
                i18n-title="Heading of an empty status list"
                title="No statuses yet."
                i18n-description="Explains what statuses are for"
                description="Create one to describe your workflow.">
                <svg emptyStateIcon size="38" lucideCircleDashed></svg>
              </app-empty-state>
            }
          </ng-template>
        </app-datatable>
      </app-page-body>
    </app-page-container>
  `,
})
export class StatusesViewComponent {
  private readonly statusesService = inject(StatusesService);
  private readonly dialog = inject(DialogService);

  readonly SortMoveDirection = SortMoveDirection;
  readonly moveUpLabel = $localize`:Tooltip on the button that moves a row up:Move up`;
  readonly moveDownLabel = $localize`:Tooltip on the button that moves a row down:Move down`;
  readonly manualOrderOnlyLabel = $localize`:Explains that manual reordering needs the default, unfiltered view:Clear the search and sort by Order to reorder`;

  readonly count = signal<number | null>(null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchInput = signal('');
  readonly sort = signal<DatatableSort | null>(null);
  private readonly reloadToken = signal(0);

  readonly search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  private readonly datatable = viewChild(DatatableComponent<Status>);

  // Rows only sit next to their sort-order neighbours in the default, unfiltered view,
  // so that is the only view where moving a row up or down means anything.
  readonly manualOrderActive = computed(() => {
    const sort = this.sort();
    const sortedByOrder = sort === null || sort.sortBy === 'sortOrder';

    return sortedByOrder && !this.search().trim();
  });

  private readonly resourceParams = computed<Params>(() => {
    const search = this.search().trim();

    return {
      entityType: EntityType.task,
      ...(search ? { search } : {}),
    };
  });

  private readonly menu: DatatableMenuItem<Status>[] = [
    {
      label: $localize`:Row action that edits a status:Edit`,
      icon: LucideSettings2,
      onClick: (status) => this.openEditDialog(status),
      disabled: () => this.saving(),
    },
    {
      label: $localize`:Row action that deletes a status:Delete`,
      icon: LucideTrash2,
      onClick: (status) => this.delete(status),
      disabled: (status) => status.isSystem || this.saving(),
    },
  ];

  readonly data: DatatableDataSource<Status> = {
    key: 'workspace-statuses',
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
        id: 'category',
        header: $localize`:Column heading for the status category:Category`,
        accessor: (status) => this.categoryLabel(status.category),
        sortable: true,
        widthClass: 'w-44',
      },
      {
        id: 'taskCount',
        header: $localize`:Column heading for the number of tasks using a row:Tasks`,
        accessor: 'taskCount',
        sortable: true,
        widthClass: 'w-24',
        cellClass: 'text-muted',
      },
      {
        id: 'sortOrder',
        header: $localize`:Column heading for the sort order:Order`,
        sortable: true,
        widthClass: 'w-28',
      },
    ],
    resource: { url: 'api/statuses/page', params: this.resourceParams },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, status: Status) => status.id,
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

    this.saving.set(true);
    this.error.set(null);

    this.statusesService
      .create({
        entityType: EntityType.task,
        name,
        category: result.category,
        color: result.color,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess || !response.payload) {
            this.error.set(response.message ?? 'Status could not be created.');
            return;
          }

          this.reload();
        },
        error: (error) =>
          this.error.set(
            getErrorMessage(error, 'Status could not be created.')
          ),
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

    this.saving.set(true);
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
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(response.message ?? 'Status could not be saved.');
            return;
          }

          this.reload();
        },
        error: (error) =>
          this.error.set(getErrorMessage(error, 'Status could not be saved.')),
      });
  }

  delete(status: Status) {
    if (status.isSystem) return;

    this.saving.set(true);
    this.error.set(null);

    this.statusesService
      .delete(status.id)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(response.message ?? 'Status could not be deleted.');
            return;
          }

          this.reload();
        },
        error: (error) =>
          this.error.set(
            getErrorMessage(error, 'Status could not be deleted.')
          ),
      });
  }

  move(statusId: number, direction: SortMoveDirection) {
    this.saving.set(true);
    this.error.set(null);

    this.statusesService
      .move({ id: statusId, direction })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.error.set(
              response.message ?? 'Statuses could not be reordered.'
            );
            return;
          }

          this.reload();
        },
        error: (error) =>
          this.error.set(
            getErrorMessage(error, 'Statuses could not be reordered.')
          ),
      });
  }

  categoryLabel(category: StatusCategory) {
    return statusCategoryLabels[category];
  }
}
