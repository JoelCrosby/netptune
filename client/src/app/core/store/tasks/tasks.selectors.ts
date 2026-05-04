import { selectTasksFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { adapter, ProjectTasksFilter, TasksState } from './tasks.model';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { netptunePermissions } from '@app/core/auth/permissions';
import { Selected } from '@core/models/selected';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { selectSelectedSprintFilterId } from '../sprints/sprints.selectors';
import {
  selectSelectedTagCount,
  selectSelectedTags,
} from '../tags/tags.selectors';
import {
  TaskStatus,
  taskStatusLabels,
  taskStatusOptions,
} from '@core/enums/project-task-status';

const { selectAll } = adapter.getSelectors();

export const selectAllTasks = createSelector(selectTasksFeature, selectAll);

export interface SelectedTaskStatus {
  status: TaskStatus;
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
  (state: TaskStatus[]) => state.length
);

export const selectSelectedAssignees = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.selectedAssignees
);

export const selectSelectedAssigneeCount = createSelector(
  selectSelectedAssignees,
  (state: string[]) => state.length
);

export const selectTaskStatusOptions = createSelector(
  selectSelectedTaskStatuses,
  (selectedStatuses): SelectedTaskStatus[] => {
    const selectedSet = new Set(selectedStatuses);

    return taskStatusOptions.map((status) => ({
      status,
      label: taskStatusLabels[status],
      selected: selectedSet.has(status),
    }));
  }
);

export const selectTaskFiltersActive = createSelector(
  selectSelectedSprintFilterId,
  selectTaskSearchTerm,
  selectSelectedTagCount,
  selectSelectedTaskStatusCount,
  selectSelectedAssigneeCount,
  (sprintId, searchTerm, tagCount, statusCount, assigneeCount) =>
    !!sprintId ||
    !!searchTerm?.trim() ||
    tagCount > 0 ||
    statusCount > 0 ||
    assigneeCount > 0
);

export const selectProjectTasksFilter = createSelector(
  selectSelectedSprintFilterId,
  selectTaskSearchTerm,
  selectSelectedTags,
  selectSelectedTaskStatuses,
  selectSelectedAssignees,
  (sprintId, search, tags, statuses, assignees): ProjectTasksFilter => ({
    sprintId,
    search: search?.trim() || undefined,
    tags: tags.length ? tags : undefined,
    statuses: statuses.length ? statuses : undefined,
    assignees: assignees.length ? assignees : undefined,
  })
);

export const selectTasksNextCursor = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.nextCursor
);

export const selectTasksPageSize = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.pageSize
);

export const selectTasksCanLoadMore = createSelector(
  selectTasksNextCursor,
  (cursor) => !!cursor
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

export const selectTasks = selectAllTasks;

export const selectTasksLoading = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.loading && !state.loaded
);

export const selectTasksLoaded = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.loaded
);

export const selectSelectedTask = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.selectedTask
);

export const selectInlineEditActive = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.inlineEditActive
);

export const selectDetailTask = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.detailTask
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

export const selectComments = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.comments
);
