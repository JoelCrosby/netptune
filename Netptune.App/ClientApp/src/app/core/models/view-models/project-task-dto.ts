export interface ProjectTaskDto {
  id: number;
  assigneeId: null;
  name: string;
  description: string;
  status: number;
  sortOrder: number;
  projectId: number;
  workspaceId: number;
  createdAt: string;
  updatedAt: string;
  assigneeUsername: string;
  ownerUsername: string;
  assigneePictureUrl: string;
  projectName: string;
}

