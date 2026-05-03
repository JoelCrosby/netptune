export interface AddSprintRequest {
  name: string;
  goal?: string | null;
  startDate: string;
  endDate: string;
  projectId: number;
}
