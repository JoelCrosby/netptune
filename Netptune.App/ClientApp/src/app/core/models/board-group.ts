import { Basemodel } from './basemodel';

export interface BoardGroup extends Basemodel {
  name: string;
  boardId: number;
  sortOrder: number;
}
