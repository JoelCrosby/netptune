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
  Basic = 0,
  Backlog = 1,
  Done = 2,
  Todo = 3,
}
