import { BoardGroupType } from '@core/models/board-group';
import { TaskStatus } from '@core/enums/project-task-status';

export const getTaskStatusFromGroupType = (
  type: BoardGroupType
): TaskStatus => {
  switch (type) {
    case BoardGroupType.Todo:
      return TaskStatus.InProgress;
    case BoardGroupType.Done:
      return TaskStatus.Complete;
    case BoardGroupType.Backlog:
      return TaskStatus.InActive;
    default:
      return TaskStatus.InActive;
  }
};

export const getGroupTypeFromTaskStatus = (
  status: TaskStatus
): BoardGroupType => {
  switch (status) {
    case TaskStatus.New:
      return BoardGroupType.Todo;
    case TaskStatus.InActive:
      return BoardGroupType.Todo;
    case TaskStatus.Complete:
      return BoardGroupType.Done;
    case TaskStatus.AwaitingClassification:
      return BoardGroupType.Todo;
    case TaskStatus.UnAssigned:
      return BoardGroupType.Backlog;
    default:
      return BoardGroupType.Backlog;
  }
};
