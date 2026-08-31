import {
  CdkDrag,
  CdkDragDrop,
  CdkDropList,
  moveItemInArray,
} from '@angular/cdk/drag-drop';
import {
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
  viewChild,
  viewChildren,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { BOARDS_HIDDEN_GROUP_IDS } from '@core/models/user-preferences';
import { BoardGroupCommandsService } from '@core/services/board-group-commands.service';
import { BoardViewService } from '@core/services/board-view.service';
import { DialogService } from '@core/services/dialog.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { getNewSortOrder } from '@core/util/sort-order-helper';
import { BoardGroupDialogComponent } from '@entry/dialogs/board-group-dialog/board-group-dialog.component';
import {
  hiddenGroupIdsForBoard,
  withBoardHiddenGroups,
} from '@boards/util/hidden-board-groups';
import {
  LucideArrowDownToLine,
  LucideArrowUpToLine,
  LucidePencil,
  LucidePlus,
  LucideSettings2,
  LucideTrash2,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { ManageBoardGroupsPreviewComponent } from './manage-board-groups-preview.component';
import {
  ManageBoardGroupRow,
  ManageBoardGroupsRowComponent,
} from './manage-board-groups-row.component';

// Past this many groups the mini board stops reading as columns, so the preview
// drops to one tick per group, rows tighten and the name filter earns its place.
const DENSE_GROUP_COUNT = 12;

@Component({
  selector: 'app-manage-board-groups-dialog',
  imports: [
    CdkDrag,
    CdkDropList,
    DialogTitleComponent,
    DialogContentComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    DropdownMenuComponent,
    FilterInputComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    ManageBoardGroupsPreviewComponent,
    ManageBoardGroupsRowComponent,
    MenuItemComponent,
    LucideArrowDownToLine,
    LucideArrowUpToLine,
    LucidePencil,
    LucidePlus,
    LucideSettings2,
    LucideTrash2,
  ],
  styles: [
    `
      .cdk-drag-placeholder {
        opacity: 0;
      }

      .cdk-drag-animating {
        transition: transform 0.2s cubic-bezier(0, 0, 0.2, 1);
      }

      .cdk-drag-preview {
        background: var(--dialog-background);
        border-radius: 6px;
        box-shadow:
          0 5px 5px -3px rgba(0, 0, 0, 0.2),
          0 8px 10px 1px rgba(0, 0, 0, 0.14),
          0 3px 14px 2px rgba(0, 0, 0, 0.12);
      }

      .group-list.cdk-drop-list-dragging
        app-manage-board-groups-row:not(.cdk-drag-placeholder) {
        transition: transform 0.2s cubic-bezier(0, 0, 0.2, 1);
      }
    `,
  ],
  template: `
    <app-dialog-title
      showCloseButton
      i18n="Title of the dialog for arranging board groups">
      Manage Groups
    </app-dialog-title>

    <app-dialog-content>
      @if (rows().length) {
        <p class="text-muted mb-3.5 text-sm">
          @if (canUpdate()) {
            <span
              i18n="
                Explains that board groups reorder and rename here, and that
                hiding one is per-user
              ">
              Drag to reorder, click a name to rename. Hiding a column only
              changes your own view of this board.
            </span>
          } @else {
            <span i18n="Explains that hiding a board group is per-user">
              Hiding a column only changes your own view of this board.
            </span>
          }
        </p>

        <app-manage-board-groups-preview
          class="mb-4"
          [rows]="rows()"
          [dense]="dense()" />

        <div
          class="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 pb-2.5">
          <span class="inline-flex items-baseline gap-1.5 whitespace-nowrap">
            <span
              class="text-[13px] font-semibold"
              i18n="
                How many board groups the board shows. VISIBLE is the visible
                count and TOTAL the total number of groups
              ">
              {{
                visibleCount() // i18n(ph="VISIBLE")
              }}
              of
              {{
                rows().length // i18n(ph="TOTAL")
              }}
              columns shown
            </span>
            @if (!dense()) {
              <span
                class="text-foreground/40 text-xs"
                i18n="
                  Notes that the list below is in the order the board draws its
                  columns
                ">
                · board order
              </span>
            }
          </span>

          <div class="flex flex-wrap items-center gap-1">
            @if (dense()) {
              <app-filter-input
                class="w-37.5"
                [value]="filterTerm()"
                [placeholder]="filterPlaceholder"
                (valueChange)="filterTerm.set($event)" />
            }

            <button
              class="h-8 min-w-0 px-2.5 text-xs"
              app-flat-button
              color="ghost"
              type="button"
              [disabled]="!canShowAll()"
              (click)="showAll()">
              <span i18n="Button that makes every board group visible">
                Show all
              </span>
            </button>
            <button
              class="h-8 min-w-0 px-2.5 text-xs"
              app-flat-button
              color="ghost"
              type="button"
              [disabled]="!canHideAll()"
              (click)="hideAll()">
              <span i18n="Button that hides every board group">Hide all</span>
            </button>
            <button
              class="h-8 min-w-0 gap-1.5 px-2.5 text-xs"
              app-flat-button
              color="ghost"
              type="button"
              [disabled]="!emptyVisibleCount()"
              (click)="hideEmpty()">
              <span i18n="Button that hides board groups containing no tasks">
                Hide empty
              </span>
              @if (emptyVisibleCount(); as count) {
                <span
                  class="bg-foreground/8 text-muted inline-flex h-4.5 min-w-4.5 items-center justify-center rounded-full px-1 text-[11px] tabular-nums">
                  {{ count }}
                </span>
              }
            </button>
          </div>
        </div>

        <div class="border-border bg-foreground/2 rounded-lg border">
          <div
            cdkDropList
            class="group-list custom-scroll flex max-h-[50vh] flex-col overflow-y-auto p-1"
            [cdkDropListDisabled]="reorderDisabled()"
            (cdkDropListDropped)="onDrop($event)">
            @for (row of filteredRows(); track row.id) {
              <app-manage-board-groups-row
                cdkDrag
                [cdkDragDisabled]="reorderDisabled()"
                [row]="row"
                [dense]="dense()"
                [canEdit]="canUpdate()"
                [reorderDisabled]="reorderDisabled()"
                (renamed)="onRenamed(row, $event)"
                (hiddenChanged)="onHiddenChanged(row, $event)"
                (menuRequested)="onMenuRequested(row, $event)" />
            } @empty {
              <p class="text-foreground/40 px-2 py-6 text-center text-sm">
                <span i18n="Empty state when a name filter matches no groups">
                  No groups match that filter.
                </span>
              </p>
            }
          </div>
        </div>
      } @else {
        <p class="text-muted text-sm">
          <span i18n="Empty state when a board has no groups">
            This board has no groups.
          </span>
        </p>
      }
    </app-dialog-content>

    <app-dropdown-menu xPosition="before">
      @if (activeRow(); as row) {
        @if (canUpdate()) {
          <button app-menu-item type="button" (click)="onRename()">
            <svg lucidePencil class="h-4 w-4 shrink-0"></svg>
            <span i18n="Menu item that renames a board group">Rename</span>
          </button>
          <button app-menu-item type="button" (click)="onStatusAndDetails()">
            <svg lucideSettings2 class="h-4 w-4 shrink-0"></svg>
            <span
              i18n="
                Menu item that opens the dialog holding a board group's status
              ">
              Status &amp; details…
            </span>
          </button>

          <div class="border-border/50 my-1 border-t"></div>

          <button
            app-menu-item
            type="button"
            [disabled]="isFirst()"
            (click)="onMoveToStart()">
            <svg lucideArrowUpToLine class="h-4 w-4 shrink-0"></svg>
            <span i18n="Menu item that moves a board group to the first column">
              Move to start
            </span>
          </button>
          <button
            app-menu-item
            type="button"
            [disabled]="isLast()"
            (click)="onMoveToEnd()">
            <svg lucideArrowDownToLine class="h-4 w-4 shrink-0"></svg>
            <span i18n="Menu item that moves a board group to the last column">
              Move to end
            </span>
          </button>
        }

        @if (canDelete()) {
          @if (canUpdate()) {
            <div class="border-border/50 my-1 border-t"></div>
          }

          <button
            app-menu-item
            type="button"
            class="text-warn!"
            (click)="onDelete()">
            <svg lucideTrash2 class="h-4 w-4 shrink-0"></svg>
            <span i18n="Menu item that deletes a board group"
              >Delete group</span
            >
          </button>
        }

        @if (!canUpdate() && !canDelete()) {
          <p class="text-foreground/40 px-3 py-2 text-sm">
            <span
              i18n="
                Shown in a board group's menu when the user may only show and
                hide it
              ">
              You can only show or hide
              {{
                row.name  // i18n(ph="NAME")
              }}.
            </span>
          </p>
        }
      }
    </app-dropdown-menu>

    <div app-dialog-actions align="end" class="items-center">
      @if (canCreate()) {
        <button
          class="mr-auto gap-2"
          app-stroked-button
          color="neutral"
          type="button"
          (click)="onCreateGroup()">
          <svg lucidePlus class="h-3.75 w-3.75"></svg>
          <span i18n="Button that creates a board group">Create Group</span>
        </button>
      }

      @if (rows().length) {
        <span
          class="text-foreground/40 text-xs"
          i18n="Notes that the dialog stores every change immediately">
          Saved as you go
        </span>
      }

      <button app-stroked-button app-dialog-close>
        <span i18n="Closes a dialog once the user has finished">Done</span>
      </button>
    </div>
  `,
})
export class ManageBoardGroupsDialogComponent {
  private readonly preferences = inject(UserPreferencesService);
  private readonly commands = inject(BoardGroupCommandsService);
  private readonly dialog = inject(DialogService);
  private readonly boardView = inject(BoardViewService);

  private readonly board = this.boardView.board;

  protected readonly canUpdate = hasPermission(PERMISSIONS.boardGroups.update);
  protected readonly canCreate = hasPermission(PERMISSIONS.boardGroups.create);
  protected readonly canDelete = hasPermission(PERMISSIONS.boardGroups.delete);

  protected readonly filterTerm = signal('');

  protected readonly filterPlaceholder = $localize`:Placeholder in the box that filters board groups by name:Filter`;

  // Reordering has to land before the board reloads, so the dialog keeps its own
  // order and falls back in step with the server on the next load.
  private readonly orderedGroups = linkedSignal(() => this.boardView.groups());

  private readonly hiddenIds = computed(() => {
    const boardId = this.board()?.id;

    if (boardId === undefined) return new Set<number>();

    const value = this.preferences.effectiveValueFor(BOARDS_HIDDEN_GROUP_IDS);

    return new Set(hiddenGroupIdsForBoard(value, boardId));
  });

  protected readonly rows = computed<ManageBoardGroupRow[]>(() => {
    const hidden = this.hiddenIds();

    return this.orderedGroups().map((group) => {
      return {
        id: group.id,
        name: group.name,
        taskCount: group.tasks.length,
        hidden: hidden.has(group.id),
      };
    });
  });

  protected readonly dense = computed(() => {
    return this.rows().length > DENSE_GROUP_COUNT;
  });

  protected readonly filteredRows = computed(() => {
    const term = this.dense() ? this.filterTerm().trim().toLowerCase() : '';

    if (!term) return this.rows();

    return this.rows().filter((row) => row.name.toLowerCase().includes(term));
  });

  // Dropping into a filtered list would place the group next to neighbours it
  // does not actually have, so the filter and dragging are mutually exclusive.
  protected readonly reorderDisabled = computed(() => {
    return (
      !this.canUpdate() || this.filteredRows().length !== this.rows().length
    );
  });

  protected readonly visibleCount = computed(() => {
    return this.rows().filter((row) => !row.hidden).length;
  });

  protected readonly emptyVisibleCount = computed(() => {
    return this.rows().filter((row) => !row.hidden && !row.taskCount).length;
  });

  protected readonly canShowAll = computed(() => this.hiddenIds().size > 0);

  protected readonly canHideAll = computed(() => this.visibleCount() > 0);

  private readonly activeGroupId = signal<number | null>(null);

  private readonly activeIndex = computed(() => {
    const id = this.activeGroupId();

    return this.orderedGroups().findIndex((group) => group.id === id);
  });

  private readonly activeGroup = computed(() => {
    return this.orderedGroups()[this.activeIndex()] ?? null;
  });

  protected readonly activeRow = computed(() => {
    return this.rows()[this.activeIndex()] ?? null;
  });

  protected readonly isFirst = computed(() => this.activeIndex() === 0);

  protected readonly isLast = computed(() => {
    return this.activeIndex() === this.orderedGroups().length - 1;
  });

  private readonly menu = viewChild.required(DropdownMenuComponent);

  private readonly rowComponents = viewChildren(ManageBoardGroupsRowComponent);

  protected onDrop(event: CdkDragDrop<unknown>) {
    this.reorder(event.previousIndex, event.currentIndex);
  }

  protected onRenamed(row: ManageBoardGroupRow, name: string) {
    this.commands.editGroup({ boardGroupId: row.id, name });
  }

  protected onHiddenChanged(row: ManageBoardGroupRow, hidden: boolean) {
    const next = new Set(this.hiddenIds());

    if (hidden) {
      next.add(row.id);
    } else {
      next.delete(row.id);
    }

    this.setHidden(next);
  }

  protected onMenuRequested(row: ManageBoardGroupRow, anchor: HTMLElement) {
    this.activeGroupId.set(row.id);
    this.menu().toggle(anchor);
  }

  protected onRename() {
    const id = this.activeGroupId();

    this.menu().close();

    const target = this.rowComponents().find((row) => row.row().id === id);

    target?.focusRename();
  }

  protected onStatusAndDetails() {
    const group = this.activeGroup();

    this.menu().close();

    if (!group) return;

    this.dialog.open(BoardGroupDialogComponent, {
      width: '600px',
      data: {
        boardId: group.boardId,
        boardGroupId: group.id,
        name: group.name,
        statusId: group.statusId,
      },
    });
  }

  protected onMoveToStart() {
    const index = this.activeIndex();

    this.menu().close();

    if (index < 0) return;

    this.reorder(index, 0);
  }

  protected onMoveToEnd() {
    const index = this.activeIndex();

    this.menu().close();

    if (index < 0) return;

    this.reorder(index, this.orderedGroups().length - 1);
  }

  protected onDelete() {
    const group = this.activeGroup();

    this.menu().close();

    if (!group) return;

    this.commands.deleteGroup(group);
  }

  protected onCreateGroup() {
    const boardId = this.board()?.id;

    if (boardId === undefined) return;

    this.dialog.open(BoardGroupDialogComponent, {
      width: '600px',
      data: { boardId },
    });
  }

  protected showAll() {
    this.setHidden(new Set());
  }

  protected hideAll() {
    this.setHidden(new Set(this.rows().map((row) => row.id)));
  }

  protected hideEmpty() {
    const empty = this.rows()
      .filter((row) => !row.taskCount)
      .map((row) => row.id);

    this.setHidden(new Set([...this.hiddenIds(), ...empty]));
  }

  private reorder(previousIndex: number, currentIndex: number) {
    if (previousIndex === currentIndex) return;

    const next = [...this.orderedGroups()];

    moveItemInArray(next, previousIndex, currentIndex);

    this.orderedGroups.set(next);

    const moved = next[currentIndex];

    const sortOrder = getNewSortOrder(
      next[currentIndex - 1]?.sortOrder,
      next[currentIndex + 1]?.sortOrder
    );

    if (moved.sortOrder === sortOrder) return;

    this.commands.editGroup({ boardGroupId: moved.id, sortOrder });
  }

  private setHidden(hidden: ReadonlySet<number>) {
    const boardId = this.board()?.id;

    if (boardId === undefined) return;

    // Prune ids for groups that no longer exist so the preference stays
    // resilient against deleted or modified groups.
    const existing = new Set(this.orderedGroups().map((group) => group.id));
    const next = [...hidden].filter((id) => existing.has(id));

    const value = this.preferences.effectiveValueFor(BOARDS_HIDDEN_GROUP_IDS);

    this.preferences
      .updateValue(
        BOARDS_HIDDEN_GROUP_IDS,
        'workspace',
        withBoardHiddenGroups(value, boardId, next)
      )
      .subscribe();
  }
}
