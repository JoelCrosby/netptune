import { Component, computed, inject, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import {
  CreateBoardComponent,
  CreateBoardDialogData,
} from '@boards/components/create-board/create-board.component';
import { workspaceBoardsResource } from '@core/resources/board.resource';
import { DialogService } from '@core/services/dialog.service';
import { delayedLoading } from '@core/util/delayed-loading';
import { BoardsViewModel } from '@core/models/view-models/boards-view-model';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { SkeletonEntityListComponent } from '@static/components/skeleton/skeleton-entity-list.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PERMISSIONS } from '@core/auth/permissions';
import { LucideKanban, LucidePlus, LucideSearchX } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
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
        @if (hasBoards()) {
          <app-search-input
            pageHeaderFilters
            [term]="query()"
            (searchChange)="query.set($event ?? '')" />
        }
      </app-page-header>

      <app-page-body scroll>
        @if (loading()) {
          @if (showSkeleton()) {
            <app-skeleton-entity-list />
          }
        } @else if (!hasBoards()) {
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
        } @else if (groups().length === 0) {
          <app-empty-state
            outlined
            compact
            i18n-title="Heading shown when the board filter matches nothing"
            title="No boards match “{{ query().trim() }}”"
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
          </div>
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class BoardsViewComponent {
  private dialog = inject(DialogService);
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;

  readonly boardsResource = workspaceBoardsResource();

  loading = this.boardsResource.isLoading;
  showSkeleton = delayedLoading(this.loading);
  boards = this.boardsResource.value;

  count = computed(() => {
    if (this.loading()) return null;

    return this.boards().reduce(
      (total, group) => total + group.boards.length,
      0
    );
  });

  hasBoards = computed(() => (this.count() ?? 0) > 0);

  canCreateBoards = hasPermission(PERMISSIONS.boards.create);

  readonly createBoardLabel = $localize`:Button that opens the create-board dialog:Create Board`;

  readonly query = signal('');
  readonly collapsed = signal<ReadonlySet<number>>(new Set());

  readonly groups = computed(() => {
    const term = this.query().trim().toLowerCase();
    const workspaceSlug = this.workspaceSlug();

    if (!workspaceSlug) {
      return [];
    }

    return this.boards()
      .map((group) => toBoardGroup(group, term, workspaceSlug))
      .filter((group) => group.items.length > 0);
  });

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

function toBoardGroup(
  group: BoardsViewModel,
  term: string,
  workspaceSlug: string | undefined
): BoardGroup {
  const projectMatches = group.projectName.toLowerCase().includes(term);

  const boards = group.boards.filter((board) => {
    if (!term || projectMatches) return true;

    const name = board.name.toLowerCase();
    const identifier = board.identifier.toLowerCase();

    return name.includes(term) || identifier.includes(term);
  });

  return {
    projectId: group.projectId,
    projectName: group.projectName,
    items: boards.map((board) => toBoardListItem(board, workspaceSlug)),
    taskCount: boards.reduce((total, board) => total + board.taskCount, 0),
  };
}
