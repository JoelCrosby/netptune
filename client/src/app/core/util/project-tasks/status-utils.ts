import { TaskStatus } from '@core/enums/project-task-status';
import { BoardGroupType } from '@core/models/view-models/board-group-view-model';

export const getTaskStatusFromGroupType = (
  type: BoardGroupType
): TaskStatus => {
  switch (type) {
    case BoardGroupType.todo:
      return TaskStatus.inProgress;
    case BoardGroupType.done:
      return TaskStatus.complete;
    case BoardGroupType.backlog:
      return TaskStatus.inActive;
    default:
      return TaskStatus.inActive;
  }
};

export const getGroupTypeFromTaskStatus = (
  status: TaskStatus
): BoardGroupType => {
  switch (status) {
    case TaskStatus.new:
      return BoardGroupType.todo;
    case TaskStatus.inActive:
      return BoardGroupType.todo;
    case TaskStatus.complete:
      return BoardGroupType.done;
    case TaskStatus.awaitingClassification:
      return BoardGroupType.todo;
    case TaskStatus.unAssigned:
      return BoardGroupType.backlog;
    default:
      return BoardGroupType.backlog;
  }
};
