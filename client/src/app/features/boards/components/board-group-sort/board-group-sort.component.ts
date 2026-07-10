import { Component, ElementRef, computed, inject } from '@angular/core';
import { selectSelectedBoard } from '@app/core/store/groups/board-groups.selectors';
import { BOARDS_TASK_SORT } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import {
  LucideArrowDown,
  LucideArrowUp,
  LucideArrowUpDown,
  LucideCheck,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';
import {
  BoardTaskSortDirection,
  BoardTaskSortField,
  boardTaskSortFieldOptions,
  boardTaskSortForBoard,
  DEFAULT_BOARD_TASK_SORT,
  withBoardTaskSort,
} from '@boards/util/board-task-sort';

@Component({
  selector: 'app-board-group-sort',
  imports: [
    FilterActionButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideArrowUp,
    LucideArrowDown,
    LucideCheck,
  ],
  template: `
    <app-filter-action-button
      label="Sort tasks"
      [icon]="lucideArrowUpDown"
      [color]="isDefault() ? undefined : 'primary'"
      [dot]="!isDefault()"
      (action)="menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu>
      <div class="min-w-44">
        @for (option of fieldOptions; track option.value) {
          <button app-menu-item (click)="onFieldClicked(option.value)">
            <svg
              lucideCheck
              class="h-4 w-4 shrink-0"
              [class.invisible]="sort().field !== option.value"></svg>
            <span class="flex-1">{{ option.label }}</span>
          </button>
        }

        @if (sort().field !== 'custom') {
          <div class="border-border my-1 border-t"></div>

          <button app-menu-item (click)="onDirectionClicked('asc')">
            <svg lucideArrowUp class="h-4 w-4 shrink-0"></svg>
            <span class="flex-1">Ascending</span>
            <svg
              lucideCheck
              class="h-4 w-4 shrink-0"
              [class.invisible]="sort().direction !== 'asc'"></svg>
          </button>

          <button app-menu-item (click)="onDirectionClicked('desc')">
            <svg lucideArrowDown class="h-4 w-4 shrink-0"></svg>
            <span class="flex-1">Descending</span>
            <svg
              lucideCheck
              class="h-4 w-4 shrink-0"
              [class.invisible]="sort().direction !== 'desc'"></svg>
          </button>
        }
      </div>
    </app-dropdown-menu>
  `,
})
export class BoardGroupSortComponent {
  readonly el = inject(ElementRef);
  private readonly store = inject(Store);
  private readonly preferences = inject(UserPreferencesService);

  readonly lucideArrowUpDown = LucideArrowUpDown;
  readonly fieldOptions = boardTaskSortFieldOptions;

  private readonly board = this.store.selectSignal(selectSelectedBoard);

  readonly sort = computed(() => {
    const boardId = this.board()?.id;

    if (boardId === undefined) return DEFAULT_BOARD_TASK_SORT;

    return boardTaskSortForBoard(
      this.preferences.effectiveValueFor(BOARDS_TASK_SORT),
      boardId
    );
  });

  readonly isDefault = computed(
    () => this.sort().field === DEFAULT_BOARD_TASK_SORT.field
  );

  onFieldClicked(field: BoardTaskSortField) {
    const current = this.sort();

    if (current.field === field) return;

    this.persist({ field, direction: current.direction });
  }

  onDirectionClicked(direction: BoardTaskSortDirection) {
    const current = this.sort();

    if (current.direction === direction) return;

    this.persist({ field: current.field, direction });
  }

  private persist(sort: {
    field: BoardTaskSortField;
    direction: BoardTaskSortDirection;
  }) {
    const boardId = this.board()?.id;

    if (boardId === undefined) return;

    const value = this.preferences.effectiveValueFor(BOARDS_TASK_SORT);

    this.preferences
      .updateValue(
        BOARDS_TASK_SORT,
        'workspace',
        withBoardTaskSort(value, boardId, sort)
      )
      .subscribe();
  }
}
