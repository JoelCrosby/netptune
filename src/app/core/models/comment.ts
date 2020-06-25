export interface Comment {
  body: string;
  entityId: number;
  entityType: entityType;
}

export enum entityType {
  task = 0,
}
