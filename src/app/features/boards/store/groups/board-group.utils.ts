import { moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { BoardGroup } from '@core/models/board-group';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { getNewSortOrder } from '@core/util/sort-order-helper';
import { getTaskStatusFromGroupType } from '@core/util/project-tasks/status-utils';
import { Update } from '@ngrx/entity';
import { adapter, BoardGroupsState } from './board-groups.model';
import { cloneDeep } from 'lodash-es';

export const moveTaskInBoardGroup = (
  state: BoardGroupsState,
  request: MoveTaskInGroupRequest
): BoardGroupsState => {
  const stateClone = cloneDeep(state);
  const newGroup = stateClone.entities[request.newGroupId];

  if (request.oldGroupId === request.newGroupId) {
    moveItemInArray(
      newGroup.tasks,
      request.previousIndex,
      request.currentIndex
    );
  } else {
    transferArrayItem(
      stateClone.entities[request.oldGroupId].tasks,
      newGroup.tasks,
      request.previousIndex,
      request.currentIndex
    );
  }

  const tasks = newGroup.tasks;

  const prevTask = tasks[request.currentIndex - 1];
  const nextTask = tasks[request.currentIndex + 1];

  const preOrder = prevTask?.sortOrder;
  const nextOrder = nextTask?.sortOrder;

  const sortOrder = getNewSortOrder(preOrder, nextOrder);

  stateClone.entities[request.newGroupId].tasks = stateClone.entities[
    request.newGroupId
  ].tasks.map((task) => {
    if (task.id !== request.taskId) {
      return task;
    }

    return {
      ...task,
      sortOrder,
      status: getTaskStatusFromGroupType(newGroup.type),
    };
  });

  return stateClone;
};

export const updateTask = (state: BoardGroupsState, task: TaskViewModel) => {
  const getGroupWithTask = (): BoardGroup | undefined => {
    for (const g of Object.values(state.entities)) {
      if (g?.tasks.findIndex((x) => x.id === task.id) !== -1) {
        return g;
      }
    }

    return undefined;
  };

  const group = getGroupWithTask();

  if (!group) return state;

  const tasks = group.tasks.map((item) => {
    if (item.id !== task.id) {
      return item;
    }

    return {
      ...task,
    };
  });

  const update: Update<BoardGroup> = {
    id: group.id,
    changes: {
      ...group,
      tasks,
    },
  };

  return adapter.updateOne(update, state);
};

export const getBulkTaskSelection = (
  state: BoardGroupsState,
  id: number,
  groupId: number
) => {
  const set = new Set(state.selectedTasks);

  const sourceGroup = state.entities[groupId];
  const siblingTasks = sourceGroup.tasks;
  const siblingIds = siblingTasks.map((task) => task.id);
  const selectedSiblingIds = siblingIds.filter((sibling) => set.has(sibling));

  // If there are no other siblings selected just add source task to selected
  if (!selectedSiblingIds.length) {
    const mod = set.add(id);
    return Array.from(mod);
  }

  // get the last selected task that is a sibling
  const getLastSelectedId = (iter = 0): number | null => {
    const target = Array.from(set)[set.size - 1 - iter];

    if (!target) return null;

    if (!selectedSiblingIds.includes(target)) {
      return getLastSelectedId(iter + 1);
    }

    return target;
  };

  const lastSelectedId = getLastSelectedId();

  if (!lastSelectedId) {
    const mod = set.add(id);
    return Array.from(mod);
  }

  const walkSiblings = (): number[] => {
    let startIndex: number = null;
    let endIndex: number = null;

    for (let i = 0; i < siblingIds.length; i++) {
      const curr = siblingIds[i];

      if (id === curr || curr === lastSelectedId) {
        if (startIndex === null) {
          startIndex = i;
          continue;
        }

        endIndex = i;
        break;
      }
    }

    const selection = siblingIds.slice(startIndex, endIndex);

    return [...selection, id];
  };

  return walkSiblings();
};
