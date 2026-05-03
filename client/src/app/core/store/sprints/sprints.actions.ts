import { HttpErrorResponse } from '@angular/common/http';
import { AddSprintRequest } from '@core/models/requests/add-sprint-request';
import { AddTasksToSprintRequest } from '@core/models/requests/add-tasks-to-sprint-request';
import { UpdateSprintRequest } from '@core/models/requests/update-sprint-request';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { createAction, props } from '@ngrx/store';
import { SprintFilter } from './sprints.model';

export const clearState = createAction('[Sprints] Clear State');

export const loadSprints = createAction(
  '[Sprints] Load Sprints',
  props<{ filter?: SprintFilter }>()
);

export const loadSprintsSuccess = createAction(
  '[Sprints] Load Sprints Success',
  props<{ sprints: SprintViewModel[]; filter?: SprintFilter }>()
);

export const loadSprintsFail = createAction(
  '[Sprints] Load Sprints Fail',
  props<{ error: HttpErrorResponse }>()
);

export const loadCurrentSprints = createAction(
  '[Sprints] Load Current Sprints'
);

export const loadCurrentSprintsSuccess = createAction(
  '[Sprints] Load Current Sprints Success',
  props<{ sprints: SprintViewModel[] }>()
);

export const loadCurrentSprintsFail = createAction(
  '[Sprints] Load Current Sprints Fail',
  props<{ error: HttpErrorResponse }>()
);

export const setSprintTaskFilter = createAction(
  '[Sprints] Set Sprint Task Filter',
  props<{ sprintId?: number }>()
);

export const loadSprintDetail = createAction(
  '[Sprints] Load Sprint Detail',
  props<{ sprintId: number }>()
);

export const loadSprintDetailSuccess = createAction(
  '[Sprints] Load Sprint Detail Success',
  props<{ sprint: SprintDetailViewModel }>()
);

export const loadSprintDetailFail = createAction(
  '[Sprints] Load Sprint Detail Fail',
  props<{ error: HttpErrorResponse }>()
);

export const loadAvailableSprintTasks = createAction(
  '[Sprints] Load Available Sprint Tasks',
  props<{ sprintId: number; projectId: number }>()
);

export const loadAvailableSprintTasksSuccess = createAction(
  '[Sprints] Load Available Sprint Tasks Success',
  props<{ tasks: TaskViewModel[] }>()
);

export const loadAvailableSprintTasksFail = createAction(
  '[Sprints] Load Available Sprint Tasks Fail',
  props<{ error: HttpErrorResponse }>()
);

export const createSprint = createAction(
  '[Sprints] Create Sprint',
  props<{ request: AddSprintRequest }>()
);

export const createSprintSuccess = createAction(
  '[Sprints] Create Sprint Success',
  props<{ sprint: SprintViewModel }>()
);

export const createSprintFail = createAction(
  '[Sprints] Create Sprint Fail',
  props<{ error: HttpErrorResponse }>()
);

export const updateSprint = createAction(
  '[Sprints] Update Sprint',
  props<{ request: UpdateSprintRequest }>()
);

export const updateSprintSuccess = createAction(
  '[Sprints] Update Sprint Success',
  props<{ sprint: SprintViewModel }>()
);

export const updateSprintFail = createAction(
  '[Sprints] Update Sprint Fail',
  props<{ error: HttpErrorResponse }>()
);

export const deleteSprint = createAction(
  '[Sprints] Delete Sprint',
  props<{ sprintId: number }>()
);

export const deleteSprintSuccess = createAction(
  '[Sprints] Delete Sprint Success',
  props<{ sprintId: number }>()
);

export const deleteSprintFail = createAction(
  '[Sprints] Delete Sprint Fail',
  props<{ error: HttpErrorResponse }>()
);

export const startSprint = createAction(
  '[Sprints] Start Sprint',
  props<{ sprintId: number }>()
);

export const completeSprint = createAction(
  '[Sprints] Complete Sprint',
  props<{ sprintId: number }>()
);

export const addTasksToSprint = createAction(
  '[Sprints] Add Tasks To Sprint',
  props<{ sprintId: number; request: AddTasksToSprintRequest }>()
);

export const removeTaskFromSprint = createAction(
  '[Sprints] Remove Task From Sprint',
  props<{ sprintId: number; taskId: number }>()
);
