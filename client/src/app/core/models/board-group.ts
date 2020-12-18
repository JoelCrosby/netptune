import { Basemodel } from './basemodel';
import { TaskViewModel } from './view-models/project-task-dto';

export interface BoardGroup extends Basemodel {
  name: string;
  boardId: number;
  sortOrder: number;
  type: BoardGroupType;
  tasks?: TaskViewModel[];
}

export enum BoardGroupType {
  basic = 0,
  backlog = 1,
  done = 2,
  todo = 3,
}
