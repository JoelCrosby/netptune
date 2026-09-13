import { Component, computed, inject, viewChild } from '@angular/core';
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
  StatusDialogComponent,
  StatusDialogResult,
} from '@entry/dialogs/status-dialog/status-dialog.component';
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
            i18n-title="Shown when a change to a status could not be saved"
            title="That change could not be saved"
            [description]="list.error() ?? ''"
            (retry)="list.reload()" />
        }

        <app-datatable
          #table
          autoFill
          stickyHeader
          tableClass="md:min-w-[720px] table-fixed"
          i18n-errorMessage="Shown when the status list fails to load"
          errorMessage="Statuses could not be loaded."
          i18n-itemLabel="Plural noun for statuses, used in the row summary"
          itemLabel="statuses"
          [data]="data"
          [(sort)]="list.sort">
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
                [appTooltip]="list.moveTooltip(list.moveUpLabel)"
                i18n-aria-label="
                  Accessible label for the button that moves a status up
                "
                aria-label="Move status up"
                [disabled]="!list.canMoveUp(i)"
                (click)="move(status.id, SortMoveDirection.up)">
                <svg lucideArrowUp class="h-4 w-4"></svg>
              </button>
              <button
                app-icon-button
                [appTooltip]="list.moveTooltip(list.moveDownLabel)"
                i18n-aria-label="
                  Accessible label for the button that moves a status down
                "
                aria-label="Move status down"
                [disabled]="!list.canMoveDown(i)"
                (click)="move(status.id, SortMoveDirection.down)">
                <svg lucideArrowDown class="h-4 w-4"></svg>
              </button>
            </div>
          </ng-template>

          <ng-template appDatatableEmpty>
            @if (list.search()) {
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

  private readonly datatable = viewChild(DatatableComponent<Status>);

  readonly list = orderableEntityList(this.datatable);

  private readonly resourceParams = computed<Params>(() => {
    return {
      entityType: EntityType.task,
      ...this.list.searchParams(),
    };
  });

  private readonly menu: DatatableMenuItem<Status>[] = [
    {
      label: $localize`:Row action that edits a status:Edit`,
      icon: LucideSettings2,
      onClick: (status) => this.openEditDialog(status),
      disabled: () => this.list.pending(),
    },
    {
      label: $localize`:Row action that deletes a status:Delete`,
      icon: LucideTrash2,
      onClick: (status) => this.delete(status),
      disabled: (status) => status.isSystem || this.list.pending(),
    },
  ];

  readonly data: DatatableDataSource<Status> = {
    key: 'workspace-statuses',
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
        id: 'category',
        header: $localize`:Column heading for the status category:Category`,
        visibleOnMobile: false,
        accessor: (status) => this.categoryLabel(status.category),
        sortable: true,
        widthClass: 'w-44',
      },
      {
        id: 'taskCount',
        header: $localize`:Column heading for the number of tasks using a row:Tasks`,
        visibleOnMobile: true,
        accessor: 'taskCount',
        sortable: true,
        widthClass: 'w-24',
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
    resource: { url: 'api/statuses/page', params: this.resourceParams },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, status: Status) => status.id,
    menu: this.menu,
    reloadSignal: this.list.reloadToken,
  };

  async openCreateDialog() {
    const result = await this.dialog.openForResult<StatusDialogResult>(
      StatusDialogComponent,
      { width: '420px' }
    );

    if (!result) return;

    this.create(result);
  }

  create(result: StatusDialogResult) {
    const name = result.name.trim();
    if (!name) return;

    const request = this.statusesService.create({
      entityType: EntityType.task,
      name,
      category: result.category,
      color: result.color,
    });

    this.list.create(request, 'Status could not be created.');
  }

  async openEditDialog(status: Status) {
    const result = await this.dialog.openForResult<StatusDialogResult, Status>(
      StatusDialogComponent,
      { data: status, width: '420px' }
    );

    if (!result) return;

    this.update(status, result);
  }

  update(status: Status, result: StatusDialogResult) {
    const name = result.name.trim();
    if (!name) return;

    const request = this.statusesService.update({
      id: status.id,
      entityType: status.entityType,
      name,
      description: status.description?.trim() || null,
      color: result.color,
      category: result.category,
    });

    this.list.save(request, 'Status could not be saved.');
  }

  delete(status: Status) {
    if (status.isSystem) return;

    const request = this.statusesService.delete(status.id);

    this.list.save(request, 'Status could not be deleted.');
  }

  move(statusId: number, direction: SortMoveDirection) {
    const request = this.statusesService.move({ id: statusId, direction });

    this.list.save(request, 'Statuses could not be reordered.');
  }

  categoryLabel(category: StatusCategory) {
    return statusCategoryLabels[category];
  }
}
