import { BoardGroupsState } from './board-groups.model';
import { MoveTaskInGroupRequest } from '@app/core/models/move-task-in-group-request';
import { moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { getNewSortOrder } from '@app/core/util/sort-order-helper';
import { getTaskStatusFromGroupType } from '@core/util/project-tasks/status-utils';
import { TaskViewModel } from '@app/core/models/view-models/project-task-dto';
import { BoardGroup } from '@app/core/models/board-group';

export const moveTaskInBoardGroup = (
  state: BoardGroupsState,
  request: MoveTaskInGroupRequest
): BoardGroupsState => {
  const newGroup = state.entities[request.newGroupId];

  if (request.oldGroupId === request.newGroupId) {
    moveItemInArray(
      newGroup.tasks,
      request.previousIndex,
      request.currentIndex
    );
  } else {
    transferArrayItem(
      state.entities[request.oldGroupId].tasks,
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

  state.entities[request.newGroupId].tasks = state.entities[
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

  return state;
};

export const updateTask = (state: BoardGroupsState, task: TaskViewModel) => {
  const getGroupWithTask = () => {
    for (const g of Object.values(state.entities)) {
      if (g.tasks.findIndex((x) => x.id === task.id) !== -1) {
        return g;
      }
    }

    return undefined;
  };

  const group: BoardGroup = getGroupWithTask();

  group.tasks = group.tasks.map((item) => {
    if (item.id !== task.id) {
      return item;
    }

    return {
      ...task,
    };
  });

  return state;
};
