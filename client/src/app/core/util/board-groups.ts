import { moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { Status } from '@core/models/status';
import { BoardViewGroup } from '@core/models/view-models/board-view';
import { getNewSortOrder } from '@core/util/sort-order-helper';

export const sortBySortOrder = (a: BoardViewGroup, b: BoardViewGroup): number =>
  a.sortOrder - b.sortOrder;

export const moveTaskInGroups = (
  groups: BoardViewGroup[],
  request: MoveTaskInGroupRequest,
  status?: Status | null
): BoardViewGroup[] => {
  const next = structuredClone(groups);

  const newGroup = next.find((group) => group.id === request.newGroupId);
  const oldGroup = next.find((group) => group.id === request.oldGroupId);

  if (!newGroup || !oldGroup) return groups;

  if (request.oldGroupId === request.newGroupId) {
    moveItemInArray(
      newGroup.tasks,
      request.previousIndex,
      request.currentIndex
    );
  } else {
    transferArrayItem(
      oldGroup.tasks,
      newGroup.tasks,
      request.previousIndex,
      request.currentIndex
    );
  }

  const prevTask = newGroup.tasks[request.currentIndex - 1];
  const nextTask = newGroup.tasks[request.currentIndex + 1];

  const sortOrder = getNewSortOrder(prevTask?.sortOrder, nextTask?.sortOrder);

  newGroup.tasks = newGroup.tasks.map((task) => {
    if (task.id !== request.taskId) {
      return task;
    }

    // When the target group assigns a status, apply it to the moved task so the
    // card reflects the new status before the board reloads.
    if (status) {
      return {
        ...task,
        sortOrder,
        statusId: status.id,
        statusName: status.name,
        statusKey: status.key,
        statusColor: status.color,
        statusCategory: status.category,
      };
    }

    return { ...task, sortOrder };
  });

  return next;
};

export const getBulkTaskSelection = (
  group: BoardViewGroup | undefined,
  selected: number[],
  id: number
): number[] => {
  const set = new Set(selected);

  if (!group) return [];

  const siblingIds = group.tasks.map((task) => task.id);
  const selectedSiblingIds = siblingIds.filter((sibling) => set.has(sibling));

  // If there are no other siblings selected just add source task to selected
  if (!selectedSiblingIds.length) {
    return Array.from(set.add(id));
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
    return Array.from(set.add(id));
  }

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

  return [...siblingIds.slice(startIndex, endIndex), id];
};
