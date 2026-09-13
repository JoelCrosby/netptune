import { Component, inject, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DialogService } from '@core/services/dialog.service';
import { TagCommandsService } from '@core/services/tag-commands.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import {
  TagDialogComponent,
  TagDialogResult,
} from '@entry/dialogs/tag-dialog/tag-dialog.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableMenuItem,
} from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { Tag } from '@core/models/tag';
import { LucideSettings2, LucideTags, LucideX } from '@lucide/angular';
import { searchableEntityList } from '../../shared/orderable-entity-list';

@Component({
  selector: 'app-tags-view',
  imports: [
    RouterLink,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    LucideTags,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    SearchInputComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for workspace tags"
        title="Tags"
        i18n-actionTitle="Button that opens the create-tag dialog"
        actionTitle="Create tag"
        i18n-filtersLabel="Accessible name of the tag list filter row"
        filtersLabel="Filter tags"
        [count]="table.loadedCount()"
        (actionClick)="openCreateDialog()">
        <div pageHeaderFilters class="flex flex-row items-center gap-2.5">
          <app-search-input
            [term]="list.searchInput()"
            (searchChange)="list.searchInput.set($event ?? '')" />
        </div>
      </app-page-header>

      <app-page-body>
        <app-datatable
          #table
          autoFill
          stickyHeader
          tableClass="table-fixed"
          i18n-errorMessage="Shown when the tag list fails to load"
          errorMessage="Tags could not be loaded."
          i18n-itemLabel="Plural noun for tags, used in the row summary"
          itemLabel="tags"
          [data]="data"
          [(sort)]="list.sort">
          <ng-template appDatatableCell="name" let-tag>
            <a
              class="block w-full truncate text-left font-medium hover:underline"
              [routerLink]="[tag.id]">
              {{ tag.name }}
            </a>
          </ng-template>

          <ng-template appDatatableEmpty>
            @if (list.search()) {
              <app-empty-state
                compact
                i18n-title="Heading shown when a search matches nothing"
                title="No tags match your search."
                i18n-description="Advice shown when a search matches nothing"
                description="Try a different term.">
                <svg emptyStateIcon size="38" lucideTags></svg>
              </app-empty-state>
            } @else {
              <app-empty-state
                compact
                i18n-title="Heading of an empty tag list"
                title="No tags yet."
                i18n-description="Explains what tags are for"
                description="Create one to group tasks across projects.">
                <svg emptyStateIcon size="38" lucideTags></svg>
              </app-empty-state>
            }
          </ng-template>
        </app-datatable>
      </app-page-body>
    </app-page-container>
  `,
})
export class TagsViewComponent {
  private readonly tagCommands = inject(TagCommandsService);
  private readonly dialog = inject(DialogService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly datatable = viewChild(DatatableComponent<Tag>);

  readonly list = searchableEntityList(this.datatable);

  private readonly menu: DatatableMenuItem<Tag>[] = [
    {
      label: $localize`:Row action that edits a tag:Edit tag`,
      icon: LucideSettings2,
      onClick: (tag) => this.openEditDialog(tag),
    },
    {
      label: $localize`:Row action that deletes a tag:Delete tag`,
      icon: LucideX,
      onClick: (tag) => this.onDeleteClicked(tag),
    },
  ];

  readonly data: DatatableDataSource<Tag> = {
    key: 'workspace-tags',
    columns: [
      {
        id: 'name',
        header: $localize`:Column heading for the name:Name`,
        visibleOnMobile: true,
        accessor: 'name',
        sortable: true,
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
    ],
    resource: { url: 'api/tags/page', params: this.list.searchParams },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, tag: Tag) => tag.id,
    menu: this.menu,
    reloadSignal: this.workspaceRefresh.version('tags'),
  };

  async openCreateDialog() {
    const result = await this.dialog.openForResult<TagDialogResult>(
      TagDialogComponent,
      { width: '420px' }
    );

    const name = result?.name.trim();
    if (!name) return;

    this.tagCommands.create(name);
  }

  async openEditDialog(tag: Tag) {
    const result = await this.dialog.openForResult<TagDialogResult, Tag>(
      TagDialogComponent,
      { data: tag, width: '420px' }
    );

    const newValue = result?.name.trim();
    if (!newValue || newValue === tag.name) return;

    this.tagCommands.rename(tag.name, newValue);
  }

  onDeleteClicked(tag: Tag) {
    if (!tag) return;

    const tags = [tag.name];
    this.tagCommands.delete(tags);
  }
}
