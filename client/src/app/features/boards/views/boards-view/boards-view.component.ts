import {
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import {
  CreateBoardComponent,
  CreateBoardDialogData,
} from '@boards/components/create-board/create-board.component';
import { workspaceBoardsPageResource } from '@core/resources/board.resource';
import { DialogService } from '@core/services/dialog.service';
import { delayedLoading } from '@core/util/delayed-loading';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { hasNextPage, Page } from '@core/models/pagination';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { SkeletonEntityListComponent } from '@static/components/skeleton/skeleton-entity-list.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PERMISSIONS } from '@core/auth/permissions';
import { LucideKanban, LucidePlus, LucideSearchX } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { CollapsibleGroupComponent } from '@static/components/collapsible-group/collapsible-group.component';
import { EntityListComponent } from '@static/components/entity-list/entity-list.component';
import { EntityListItem } from '@static/components/entity-list/entity-list.types';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { toBoardListItem } from '@boards/util/board-list-item';

interface BoardGroup {
  projectId: number;
  projectName: string;
  items: EntityListItem[];
  taskCount: number;
}

@Component({
  selector: 'app-boards-view',
  imports: [
    SkeletonEntityListComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    SearchInputComponent,
    CollapsibleGroupComponent,
    EntityListComponent,
    LucideKanban,
    LucidePlus,
    LucideSearchX,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the board list"
        title="Boards"
        i18n-filtersLabel="Accessible name of the board list filter row"
        filtersLabel="Filter boards"
        [actionTitle]="canCreateBoards() ? createBoardLabel : null"
        [count]="count()"
        (actionClick)="onCreateBoardClicked()">
        @if (hasBoards() || query()) {
          <app-search-input
            pageHeaderFilters
            [term]="query()"
            (searchChange)="query.set($event ?? '')" />
        }
      </app-page-header>

      <app-page-body scroll>
        @if (initialLoad()) {
          @if (showSkeleton()) {
            <app-skeleton-entity-list />
          }
        } @else if (!hasBoards() && !query()) {
          <app-empty-state
            i18n-title="Heading of the empty board list"
            title="There are currently no boards."
            i18n-description="
              Explains what a board is for, on the empty board list
            "
            description="Create your first board to organise and track work for a project.">
            <svg emptyStateIcon size="38" lucideKanban></svg>

            @if (canCreateBoards()) {
              <button
                emptyStateAction
                app-flat-button
                type="button"
                (click)="onCreateBoardClicked()">
                <svg size="20" lucidePlus></svg>
                <span i18n="Button that opens the create-board dialog">
                  Create Board
                </span>
              </button>
            }
          </app-empty-state>
        } @else if (!hasBoards()) {
          <app-empty-state
            outlined
            compact
            i18n-title="Heading shown when the board filter matches nothing"
            title="No boards match “{{ query() }}”"
            i18n-description="
              Advice shown when the board filter matches nothing
            "
            description="Try a project name, or create a new board.">
            <svg emptyStateIcon size="32" lucideSearchX></svg>
          </app-empty-state>
        } @else {
          <div class="flex flex-col gap-6">
            @for (group of groups(); track group.projectId) {
              <app-collapsible-group
                [label]="group.projectName"
                [count]="group.items.length"
                [meta]="taskCountLabel(group.taskCount)"
                [expanded]="!collapsed().has(group.projectId)"
                (expandedChange)="setExpanded(group.projectId, $event)">
                <app-entity-list
                  i18n-nameHeading="Column heading for the board name"
                  nameHeading="Board"
                  i18n-countHeading="
                    Column heading for the number of tasks on a board
                  "
                  countHeading="Tasks"
                  [items]="group.items"
                  [actionLabel]="createInProjectLabel(group.projectName)"
                  (actionClick)="onCreateBoardClicked(group.projectId)" />
              </app-collapsible-group>
            }

            @if (hasMore()) {
              <button
                app-stroked-button
                type="button"
                class="self-center"
                [disabled]="loading()"
                (click)="loadMore()">
                <span i18n="Button that loads the next page of boards">
                  Load more boards
                </span>
              </button>
            }
          </div>
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class BoardsViewComponent {
  private dialog = inject(DialogService);
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;

  readonly query = signal('');

  // Back to the first page whenever the search or the workspace changes.
  readonly page = linkedSignal(() => {
    this.query();
    this.workspaceSlug();

    return 1;
  });

  readonly boardsResource = workspaceBoardsPageResource(this.query, this.page);

  loading = this.boardsResource.isLoading;

  // The last page to arrive, kept while the next one loads so the list does
  // not blank out between searches.
  private readonly lastPage = linkedSignal<
    Page<BoardViewModel> | undefined,
    Page<BoardViewModel> | undefined
  >({
    source: () => this.boardsResource.value()?.payload,
    computation: (page, previous) => page ?? previous?.value,
  });

  // Every page loaded so far for the current search. The first page replaces
  // the list; later pages append to it.
  private readonly boards = linkedSignal<
    Page<BoardViewModel> | undefined,
    BoardViewModel[]
  >({
    source: () => this.boardsResource.value()?.payload,
    computation: (page, previous) => {
      const loaded = previous?.value ?? [];

      if (!page) return loaded;
      if (page.page === 1) return page.items;

      return mergeBoards(loaded, page.items);
    },
  });

  initialLoad = computed(() => this.loading() && !this.lastPage());
  showSkeleton = delayedLoading(this.initialLoad);

  count = computed(() => this.lastPage()?.totalCount ?? null);
  hasBoards = computed(() => (this.count() ?? 0) > 0);
  hasMore = computed(() => hasNextPage(this.lastPage()));

  canCreateBoards = hasPermission(PERMISSIONS.boards.create);

  readonly createBoardLabel = $localize`:Button that opens the create-board dialog:Create Board`;

  readonly collapsed = signal<ReadonlySet<number>>(new Set());

  readonly groups = computed(() => {
    return groupByProject(this.boards(), this.workspaceSlug());
  });

  loadMore() {
    this.page.update((page) => page + 1);
  }

  setExpanded(projectId: number, expanded: boolean) {
    this.collapsed.update((collapsed) => {
      const next = new Set(collapsed);

      if (expanded) {
        next.delete(projectId);
      } else {
        next.add(projectId);
      }

      return next;
    });
  }

  taskCountLabel(count: number) {
    return $localize`:Total tasks across the boards in a project group:${count}:count: tasks`;
  }

  createInProjectLabel(projectName: string) {
    if (!this.canCreateBoards()) return null;

    return $localize`:Row that opens the create-board dialog for one project:Create board in ${projectName}:projectName:`;
  }

  onCreateBoardClicked(projectId?: number) {
    const data: CreateBoardDialogData = { projectId };

    this.dialog.open(CreateBoardComponent, {
      width: '600px',
      data,
    });
  }
}

// The API orders boards by project, so each project's boards arrive together.
function groupByProject(
  boards: readonly BoardViewModel[],
  workspaceSlug: string | undefined
): BoardGroup[] {
  const groups = new Map<number, BoardGroup>();

  for (const board of boards) {
    const group = groups.get(board.projectId) ?? {
      projectId: board.projectId,
      projectName: board.projectName,
      items: [],
      taskCount: 0,
    };

    group.items.push(toBoardListItem(board, workspaceSlug));
    group.taskCount += board.taskCount;

    groups.set(board.projectId, group);
  }

  return [...groups.values()];
}

// A reload of a later page can return boards already on the list; the newer
// copy wins.
function mergeBoards(
  loaded: readonly BoardViewModel[],
  incoming: readonly BoardViewModel[]
): BoardViewModel[] {
  const incomingIds = new Set(incoming.map((board) => board.id));
  const kept = loaded.filter((board) => !incomingIds.has(board.id));

  return [...kept, ...incoming];
}
