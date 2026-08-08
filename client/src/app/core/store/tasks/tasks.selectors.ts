import { selectTasksFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { adapter, ProjectTasksFilter, TasksState } from './tasks.model';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { netptunePermissions } from '@app/core/auth/permissions';
import { Selected } from '@core/models/selected';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import {
  selectSelectedTagCount,
  selectSelectedTags,
} from '../tags/tags.selectors';

const { selectAll } = adapter.getSelectors();

export const selectAllTasks = createSelector(selectTasksFeature, selectAll);

export interface SelectedTaskStatus {
  status: number;
  label: string;
  selected: boolean;
}

export const selectTaskSearchTerm = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.searchTerm
);

export const selectSelectedTaskStatuses = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.selectedStatuses
);

export const selectSelectedTaskStatusCount = createSelector(
  selectSelectedTaskStatuses,
  (state: number[]) => state.length
);

export const selectSelectedAssignees = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.selectedAssignees
);

export const selectSelectedAssigneeCount = createSelector(
  selectSelectedAssignees,
  (state: string[]) => state.length
);

export const selectSelectedTaskIds = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.selectedTaskIds
);

export const selectTaskStatusOptions = createSelector(
  selectSelectedTaskStatuses,
  (selectedStatuses): Set<number> => {
    return new Set(selectedStatuses);
  }
);

/** The sprint filter is held outside the store, so callers add it themselves. */
export const selectTaskFiltersActive = createSelector(
  selectTaskSearchTerm,
  selectSelectedTagCount,
  selectSelectedTaskStatusCount,
  selectSelectedAssigneeCount,
  (searchTerm, tagCount, statusCount, assigneeCount) =>
    !!searchTerm?.trim() || tagCount > 0 || statusCount > 0 || assigneeCount > 0
);

export const selectProjectTasksFilter = createSelector(
  selectTaskSearchTerm,
  selectSelectedTags,
  selectSelectedTaskStatuses,
  selectSelectedAssignees,
  (
    search,
    tags,
    statuses,
    assignees
  ): Omit<ProjectTasksFilter, 'sprintId'> => ({
    search: search?.trim() || undefined,
    tags: tags.length ? tags : undefined,
    statusIds: statuses.length ? statuses : undefined,
    assignees: assignees.length ? assignees : undefined,
  })
);

export const selectTaskAssigneeOptions = createSelector(
  selectAllTasks,
  selectSelectedAssignees,
  (tasks, selectedAssignees): Selected<AssigneeViewModel>[] => {
    const selectedSet = new Set(selectedAssignees);
    const assigneeMap = tasks
      .flatMap((task) => task.assignees)
      .reduce((map, assignee) => {
        if (!map.has(assignee.id)) {
          map.set(assignee.id, assignee);
        }

        return map;
      }, new Map<string, AssigneeViewModel>());

    return Array.from(assigneeMap.values())
      .sort((a, b) => a.displayName.localeCompare(b.displayName))
      .map((assignee) => ({
        ...assignee,
        selected: selectedSet.has(assignee.id),
      }));
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
