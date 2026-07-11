import { AddSprintRequest } from '@core/models/requests/add-sprint-request';
import { AddTasksToSprintRequest } from '@core/models/requests/add-tasks-to-sprint-request';
import { UpdateSprintRequest } from '@core/models/requests/update-sprint-request';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';
import { Params } from '@angular/router';
import { SprintFilter } from './sprints.model';

export const clearState = createAction('[Sprints] Clear State');

// Load Sprints

export const loadSprints = createAsyncAction('[Sprints] Load Sprints', {
  init: props<{ filter?: SprintFilter }>(),
  success: props<{ sprints: SprintViewModel[]; filter?: SprintFilter }>(),
});

// Load Current Sprints

export const loadCurrentSprints = createAsyncAction(
  '[Sprints] Load Current Sprints',
  {
    success: props<{ sprints: SprintViewModel[] }>(),
  }
);

// Sync filter setter

export const setSprintTaskFilter = createAction(
  '[Sprints] Set Sprint Task Filter',
  props<{ sprintId?: number }>()
);

// Load Sprint Detail

export const loadSprintDetail = createAsyncAction(
  '[Sprints] Load Sprint Detail',
  {
    init: props<{ sprintId: number }>(),
    success: props<{ sprint: SprintDetailViewModel }>(),
  }
);

// Create Sprint

export const createSprint = createAsyncAction('[Sprints] Create Sprint', {
  init: props<{ request: AddSprintRequest }>(),
  success: props<{ sprint: SprintViewModel }>(),
});

// Update Sprint

export const updateSprint = createAsyncAction('[Sprints] Update Sprint', {
  init: props<{ request: UpdateSprintRequest }>(),
  success: props<{ sprint: SprintViewModel }>(),
});

// Delete Sprint

export const deleteSprint = createAsyncAction('[Sprints] Delete Sprint', {
  init: props<{ sprintId: number }>(),
  success: props<{ sprintId: number }>(),
});

// Sprint lifecycle triggers

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

export const completeSprintWithReassignment = createAction(
  '[Sprints] Complete Sprint With Reassignment',
  props<{
    sprintId: number;
    incompleteTaskIds: number[];
    targetSprintId?: number;
  }>()
);

export const initBacklogView = createAction('[Sprints] Init Backlog View');

export const updateBacklogTaskFilter = createAction(
  '[Sprints] Update Backlog Task Filter',
  props<{ params: Params }>()
);

export const assignBacklogTask = createAction(
  '[Sprints] Assign Backlog Task',
  props<{ taskId: number; sprintId: number }>()
);
