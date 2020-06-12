import { BoardGroupsState } from './board-groups.model';
import { MoveTaskInGroupRequest } from '@app/core/models/move-task-in-group-request';
import { moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { getNewSortOrder } from '@app/core/util/sort-order-helper';
import { getTaskStatusFromGroupType } from '@app/core/project-tasks/status-utils';

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
    if (task.id !== request.taskId) return task;

    return {
      ...task,
      sortOrder,
      status: getTaskStatusFromGroupType(newGroup.type),
    };
  });

  return state;
};
