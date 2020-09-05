import { moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { BoardGroup } from '@app/core/models/board-group';
import { MoveTaskInGroupRequest } from '@app/core/models/move-task-in-group-request';
import { TaskViewModel } from '@app/core/models/view-models/project-task-dto';
import { getNewSortOrder } from '@app/core/util/sort-order-helper';
import { getTaskStatusFromGroupType } from '@core/util/project-tasks/status-utils';
import { Update } from '@ngrx/entity';
import { adapter, BoardGroupsState } from './board-groups.model';
import { cloneDeep } from 'lodash';

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
