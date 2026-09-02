import { computed, inject, Service, linkedSignal } from '@angular/core';
import { Selected } from '@core/models/selected';
import {
  BoardView,
  BoardViewGroup,
  BoardViewTask,
} from '@core/models/view-models/board-view';
import { BoardViewService } from '@core/services/board-view.service';
import { getBulkTaskSelection } from '@core/util/board-groups';

@Service()
export class BoardSelectionService {
  private readonly boardView = inject(BoardViewService);

  // Sourced from the board as loaded rather than as held, so that a fresh load
  // clears the selection but a drag, which writes to the held board, does not.
  private readonly selection = linkedSignal<BoardView | undefined, number[]>({
    source: this.boardView.loadedBoard,
    computation: (loaded, previous) => (loaded ? [] : (previous?.value ?? [])),
  });

  readonly taskIds = this.selection.asReadonly();
  readonly count = computed(() => this.selection().length);

  readonly selectedTasks = computed(() => {
    const selected = new Set(this.taskIds());

    return this.boardView
      .groups()
      .flatMap((group) => group.tasks)
      .filter((task) => selected.has(task.id));
  });

  readonly groups = computed<BoardViewGroupWithSelection[]>(() => {
    const selected = new Set(this.taskIds());

    return this.boardView.groups().map((group) => {
      return {
        ...group,
        tasks: group.tasks.map((task) => {
          return { ...task, selected: selected.has(task.id) };
        }),
      };
    });
  });

  select(id: number) {
    this.selection.update((selected) => Array.from(new Set(selected).add(id)));
  }

  deselect(id: number) {
    this.selection.update((selected) => selected.filter((task) => task !== id));
  }

  selectRange(id: number, groupId: number) {
    this.selection.update((selected) => {
      const range = this.range(selected, id, groupId);

      return Array.from(new Set([...selected, ...range]));
    });
  }

  deselectRange(id: number, groupId: number) {
    this.selection.update((selected) => {
      const range = new Set(this.range(selected, id, groupId));

      return selected.filter((task) => !range.has(task));
    });
  }

  clear() {
    this.selection.set([]);
  }

  private range(selected: number[], id: number, groupId: number) {
    const group = this.boardView.groups().find((item) => item.id === groupId);

    return getBulkTaskSelection(group, selected, id);
  }
}

export type BoardViewGroupWithSelection = Omit<BoardViewGroup, 'tasks'> & {
  tasks: Selected<BoardViewTask>[];
};
