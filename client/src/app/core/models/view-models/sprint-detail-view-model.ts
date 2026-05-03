import { EstimateType } from '@core/enums/estimate-type';
import { TaskViewModel } from './project-task-dto';
import { SprintViewModel } from './sprint-view-model';

export interface SprintDetailViewModel extends SprintViewModel {
  newTaskCount: number;
  activeTaskCount: number;
  doneTaskCount: number;
  totalEstimateValue?: number | null;
  estimateType?: EstimateType | null;
  tasks: TaskViewModel[];
}
