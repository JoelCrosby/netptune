export interface ProjectViewModel {
  id: number;
  key: string;
  name: string;
  description: string;
  repositoryUrl: string;
  workspaceId: number;
  ownerDisplayName: string;
  updatedAt: Date;
  createdAt: Date;
  defaultBoardIdentifier: string;
}
