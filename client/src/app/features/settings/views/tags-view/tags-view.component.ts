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
import { DialogService } from '@core/services/dialog.service';
import { TagCommandsService } from '@core/services/tag-commands.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import {
  CreateTagDialogComponent,
  CreateTagDialogResult,
} from '@entry/dialogs/create-tag-dialog/create-tag-dialog.component';
import {
  EditTagDialogComponent,
  EditTagDialogResult,
} from '@entry/dialogs/edit-tag-dialog/edit-tag-dialog.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableMenuItem,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { Tag } from '@core/models/tag';
import { LucideSettings2, LucideTags, LucideX } from '@lucide/angular';
import { debounceTime } from 'rxjs/operators';
import { first } from 'rxjs';

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
        [count]="count()"
        (actionClick)="openCreateDialog()">
        <div pageHeaderFilters class="flex flex-row items-center gap-2.5">
          <app-search-input
            [term]="searchInput()"
            (searchChange)="searchInput.set($event ?? '')" />
        </div>
      </app-page-header>

      <app-page-body>
        <app-datatable
          autoFill
          stickyHeader
          tableClass="table-fixed"
          i18n-errorMessage="Shown when the tag list fails to load"
          errorMessage="Tags could not be loaded."
          i18n-itemLabel="Plural noun for tags, used in the row summary"
          itemLabel="tags"
          [data]="data"
          [(sort)]="sort"
          (loaded)="count.set($event.hasValue ? $event.totalCount : null)">
          <ng-template appDatatableCell="name" let-tag>
            <a
              class="block w-full truncate text-left font-medium hover:underline"
              [routerLink]="[tag.id]">
              {{ tag.name }}
            </a>
          </ng-template>

          <ng-template appDatatableEmpty>
            @if (search()) {
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

  readonly count = signal<number | null>(null);
  readonly searchInput = signal('');
  readonly sort = signal<DatatableSort | null>(null);

  readonly search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  private readonly datatable = viewChild(DatatableComponent<Tag>);

  private readonly resourceParams = computed<Params>(() => {
    const search = this.search().trim();

    return search ? { search } : {};
  });

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
        accessor: 'name',
        sortable: true,
      },
      {
        id: 'taskCount',
        header: $localize`:Column heading for the number of tasks using a row:Tasks`,
        accessor: 'taskCount',
        sortable: true,
        widthClass: 'w-24',
        cellClass: 'text-muted',
      },
    ],
    resource: { url: 'api/tags/page', params: this.resourceParams },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, tag: Tag) => tag.id,
    menu: this.menu,
    reloadSignal: this.workspaceRefresh.version('tags'),
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

  openCreateDialog() {
    const dialogRef = this.dialog.open<CreateTagDialogResult>(
      CreateTagDialogComponent,
      {
        width: '420px',
      }
    );

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        const name = result?.name.trim();
        if (!name) return;

        this.tagCommands.create(name);
      },
    });
  }

  openEditDialog(tag: Tag) {
    const dialogRef = this.dialog.open<EditTagDialogResult, Tag>(
      EditTagDialogComponent,
      {
        data: tag,
        width: '420px',
      }
    );

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        const newValue = result?.name.trim();
        if (!newValue || newValue === tag.name) return;

        this.tagCommands.rename(tag.name, newValue);
      },
    });
  }

  onDeleteClicked(tag: Tag) {
    if (!tag) return;

    const tags = [tag.name];
    this.tagCommands.delete(tags);
  }
}
