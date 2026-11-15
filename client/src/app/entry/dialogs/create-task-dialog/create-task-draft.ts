import { Injectable, InjectionToken } from '@angular/core';
import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { UserSelectValue } from '@core/models/view-models/user-select-option';

export interface StagedRelation {
  relationTypeId: number;
  label: string;
  taskIsSource: boolean;
  task: TaskViewModel;
}

export interface CreateTaskDraft {
  name: string;
  description: string;
  statusId: number | null;
  priority: TaskPriority | null;
  estimateType: EstimateType | null;
  estimateValue: number | null;
  projectId: number | null;
  sprintId: number | null;
  startDate: string;
  dueDate: string;
  assignees: UserSelectValue[];
  tags: string[];
  files: File[];
  relations: StagedRelation[];
}

export interface CreateTaskDialogData {
  projectId?: number;
  sprintId?: number;
  draft?: CreateTaskDraft;
}

export const CREATE_TASK_PAGE_DATA = new InjectionToken<CreateTaskDialogData>(
  'CreateTaskPageData'
);

@Injectable({ providedIn: 'root' })
export class CreateTaskDraftService {
  private draft: CreateTaskDraft | null = null;

  stash(draft: CreateTaskDraft) {
    this.draft = draft;
  }

  take() {
    const draft = this.draft;

    this.draft = null;

    return draft;
  }
}
