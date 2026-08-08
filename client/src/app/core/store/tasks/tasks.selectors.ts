import { selectTasksFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { adapter, TasksState } from './tasks.model';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { netptunePermissions } from '@app/core/auth/permissions';
import { AssigneeViewModel } from '@core/models/view-models/board-view';

const { selectAll } = adapter.getSelectors();

export const selectAllTasks = createSelector(selectTasksFeature, selectAll);

export interface SelectedTaskStatus {
  status: number;
  label: string;
  selected: boolean;
}

export const selectSelectedTaskIds = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.selectedTaskIds
);

/** Who appears in the filter, not who is picked — the route holds the selection. */
export const selectTaskAssignees = createSelector(
  selectAllTasks,
  (tasks): AssigneeViewModel[] => {
    const assigneeMap = tasks
      .flatMap((task) => task.assignees)
      .reduce((map, assignee) => {
        if (!map.has(assignee.id)) {
          map.set(assignee.id, assignee);
        }

        return map;
      }, new Map<string, AssigneeViewModel>());

    return Array.from(assigneeMap.values()).sort((a, b) =>
      a.displayName.localeCompare(b.displayName)
    );
  }
);

export const selectTaskEditLoading = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.editState.loading
);

export const selectSelectedTask = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.selectedTask
);

export const selectDetailTask = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.detailTask
);

export const selectDetailTaskLoading = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.detailState.loading
);

export const selectDetailTaskError = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.detailState.error
);

export const selectDetailTaskIsRedOnly = createSelector(
  selectHasPermission(netptunePermissions.tasks.update),
  (state) => !state
);

export const selectRequiredDetailTask = createSelector(
  selectTasksFeature,
  (state: TasksState) => {
    if (!state.detailTask) {
      throw new Error('No task selected');
    }

    return state.detailTask;
  }
);
