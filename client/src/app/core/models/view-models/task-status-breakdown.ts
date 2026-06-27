import { StatusCategory } from '../status';

export interface TaskStatusBreakdownItem {
  statusId: number;
  name: string;
  color?: string | null;
  category: StatusCategory;
  count: number;
}

export interface TaskStatusBreakdown {
  total: number;
  statuses: TaskStatusBreakdownItem[];
}
