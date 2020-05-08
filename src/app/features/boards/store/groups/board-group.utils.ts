import { BoardGroupsState } from './board-groups.model';
import { MoveTaskInGroupRequest } from '@app/core/models/move-task-in-group-request';
import { moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { getNewSortOrder } from '@app/core/util/sort-order-helper';

export const moveTaskInBoardGroup = (
  state: BoardGroupsState,
  request: MoveTaskInGroupRequest
): BoardGroupsState => {
  if (request.oldGroupId === request.newGroupId) {
    moveItemInArray(
      state.entities[request.newGroupId].tasks,
      request.previousIndex,
      request.currentIndex
    );
  } else {
    transferArrayItem(
      state.entities[request.oldGroupId].tasks,
      state.entities[request.newGroupId].tasks,
      request.previousIndex,
      request.currentIndex
    );
  }

  const tasks = state.entities[request.newGroupId].tasks;

  const prevTask = tasks[request.currentIndex - 1];
  const nextTask = tasks[request.currentIndex + 1];

  const preOrder = prevTask?.sortOrder;
  const nextOrder = nextTask?.sortOrder;

  const sortOrder = getNewSortOrder(preOrder, nextOrder);

  state.entities[request.newGroupId].tasks = state.entities[
    request.newGroupId
  ].tasks.map((task) => {
    if (task.id !== request.taskId) return task;

    return { ...task, sortOrder };
  });

  return state;
};
