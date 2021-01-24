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
