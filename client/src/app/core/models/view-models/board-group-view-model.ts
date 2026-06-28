import { StatusCategory } from '@core/models/status';

export interface BoardGroupViewModel {
  id: number;
  name: string;
  boardId: number;
  type: BoardGroupType;
  sortOrder: number;
}

export enum BoardGroupType {
  basic = 0,
  backlog = 1,
  done = 2,
  todo = 3,
}

// Mirrors the server's BoardGroupType.GetStatusCategoryFromGroupType so the optimistic
// move update can reflect the new status category before the board reloads.
export function getStatusCategoryFromGroupType(
  type: BoardGroupType
): StatusCategory {
  switch (type) {
    case BoardGroupType.todo:
      return StatusCategory.active;
    case BoardGroupType.done:
      return StatusCategory.done;
    default:
      return StatusCategory.backlog;
  }
}
