export interface UpdateProjectRequest {
  id: number;
  name: string;
  description: string;
  repositoryUrl: string;
  key: string;
  defaultStatusId?: number | null;
}
