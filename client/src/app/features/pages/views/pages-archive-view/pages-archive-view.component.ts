import { Component, computed, inject } from '@angular/core';
import { ArchivedPage, PageKind } from '@core/models/pages';
import { PageCommandsService } from '@core/services/page-commands.service';
import {
  LucideArchiveRestore,
  LucideArchiveX,
  LucideFileText,
  LucideFolder,
} from '@lucide/angular';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableColumn,
  DatatableDataSource,
  DatatableMenuItem,
} from '@static/components/datatable/datatable.types';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';
import { archivedPagesResource } from '../../resources/pages.resource';
import { pageArchiveRights } from '../../util/page-archive-rights';

@Component({
  selector: 'app-pages-archive-view',
  imports: [
    PageBodyComponent,
    PageHeaderComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
    DatatableEmptyDirective,
    EmptyStateComponent,
    FromNowPipe,
    LucideFolder,
    LucideFileText,
  ],
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  template: `
    <app-page-header
      toolbar
      i18n-title="Title of the archived pages list"
      title="Archive"
      [count]="archived.isLoading() ? null : archived.value().length" />

    <app-page-body>
      <app-datatable
        autoFill
        i18n-errorMessage="Shown when the archived pages fail to load"
        errorMessage="Archived pages could not be loaded."
        i18n-itemLabel="
          Plural noun for the rows of the archived pages table, shown beside
          counts
        "
        itemLabel="pages"
        tableClass="table-fixed"
        [data]="data()"
        [stickyHeader]="true">
        <ng-template appDatatableCell="title" let-page>
          <span class="flex min-w-0 items-center gap-2">
            @if (page.kind === folderKind) {
              <svg lucideFolder class="text-muted h-4 w-4 shrink-0"></svg>
            } @else {
              <svg lucideFileText class="text-muted h-4 w-4 shrink-0"></svg>
            }
            <span class="truncate font-medium">{{ page.title }}</span>
          </span>
        </ng-template>

        <ng-template appDatatableCell="archivedBy" let-page>
          <span class="block truncate text-sm">
            {{ page.archivedByDisplayName ?? '—' }}
          </span>
        </ng-template>

        <ng-template appDatatableCell="archivedAt" let-page>
          <span class="text-muted text-sm whitespace-nowrap">
            {{ page.archivedAt | fromNow }}
          </span>
        </ng-template>

        <ng-template appDatatableEmpty>
          <app-empty-state
            compact
            i18n-title="Shown when no page is archived"
            title="Nothing archived"
            i18n-description="Hint on the empty page archive"
            description="Archived pages appear here, ready to be restored." />
        </ng-template>
      </app-datatable>
    </app-page-body>
  `,
})
export class PagesArchiveViewComponent {
  private readonly commands = inject(PageCommandsService);
  private readonly canManage = pageArchiveRights();

  protected readonly archived = archivedPagesResource();
  protected readonly folderKind = PageKind.folder;

  private readonly columns: DatatableColumn<ArchivedPage>[] = [
    {
      id: 'title',
      header: $localize`:Header of the archived pages column that names the page:Title`,
      visibleOnMobile: true,
    },
    {
      id: 'archivedBy',
      header: $localize`:Header of the archived pages column naming who archived the page:Archived by`,
      visibleOnMobile: false,
      widthClass: 'w-56',
    },
    {
      id: 'archivedAt',
      header: $localize`:Header of the archived pages column saying when the page was archived:Archived`,
      visibleOnMobile: true,
      widthClass: 'w-40',
    },
  ];

  private readonly restoreItem: DatatableMenuItem<ArchivedPage> = {
    label: $localize`:Row action that restores an archived page:Restore`,
    icon: LucideArchiveRestore,
    onClick: (page) => this.commands.restore(page.id),
    disabled: (page) => !this.canManage(page.createdByUserId),
  };

  private readonly deleteItem: DatatableMenuItem<ArchivedPage> = {
    label: $localize`:Row action that deletes an archived page for good:Delete permanently`,
    icon: LucideArchiveX,
    color: 'warn',
    onClick: (page) => this.commands.deletePermanently(page),
    disabled: (page) => !this.canManage(page.createdByUserId),
  };

  protected readonly data = computed<DatatableDataSource<ArchivedPage>>(() => {
    return {
      key: 'pages-archive',
      columns: this.columns,
      items: this.archived.value,
      loading: this.archived.isLoading,
      trackBy: (_: number, page: ArchivedPage) => page.id,
      menu: [this.restoreItem, this.deleteItem],
    };
  });
}
