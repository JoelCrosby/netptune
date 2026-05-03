import { SprintStatus } from '@core/enums/sprint-status';

export interface UpdateSprintRequest {
  id: number;
  name?: string;
  goal?: string | null;
  startDate?: string;
  endDate?: string;
  status?: SprintStatus;
}
