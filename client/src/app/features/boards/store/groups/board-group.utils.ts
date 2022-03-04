import { UpdateProjectTaskRequest } from '@core/models/requests/update-project-task-request';
import { moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import {
  BoardViewGroup,
  BoardViewTask,
} from '@core/models/view-models/board-view';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { getTaskStatusFromGroupType } from '@core/util/project-tasks/status-utils';
import { getNewSortOrder } from '@core/util/sort-order-helper';
import { Update } from '@ngrx/entity';
import { cloneDeep } from 'lodash-es';
import { adapter, BoardGroupsState } from './board-groups.model';

export const moveTaskInBoardGroup = (
  state: BoardGroupsState,
  request: MoveTaskInGroupRequest
): BoardGroupsState => {
  const stateClone = cloneDeep(state);
  const newGroup = stateClone.entities[request.newGroupId];

  if (!newGroup || !stateClone?.entities) return state;

  if (request.oldGroupId === request.newGroupId) {
    moveItemInArray(
      newGroup.tasks,
      request.previousIndex,
      request.currentIndex
    );
  } else {
    transferArrayItem(
      (stateClone.entities[request.oldGroupId] as unknown as BoardViewGroup)
        .tasks,
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

  (stateClone.entities[request.newGroupId] as unknown as BoardViewGroup).tasks =
    (
      stateClone.entities[request.newGroupId] as unknown as BoardViewGroup
    ).tasks.map((task) => {
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

export const updateTask = (
  state: BoardGroupsState,
  task: BoardViewTask | TaskViewModel | UpdateProjectTaskRequest
) => {
  const getGroupWithTask = (): BoardViewGroup | undefined => {
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
      ...(task as BoardViewTask),
    };
  });

  const update: Update<BoardViewGroup> = {
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
): number[] => {
  const set = new Set(state.selectedTasks);

  const sourceGroup = state.entities[groupId];

  if (!sourceGroup) return [];

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
    let startIndex: number | null = null;
    let endIndex: number | null = null;

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

    if (startIndex == null || endIndex == null) {
      throw new Error(
        'unable to determine start/end index in getBulkTaskSelection'
      );
    }

    const selection = siblingIds.slice(startIndex, endIndex);

    return [...selection, id];
  };

  return walkSiblings();
};
