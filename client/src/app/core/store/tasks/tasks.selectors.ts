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
    statusIds: statuses.length ? statuses : undefined,
    assignees: assignees.length ? assignees : undefined,
  })
);

export const selectTasksPage = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.page
);

export const selectTasksPageSize = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.pageSize
);

export const selectTasksTotalCount = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.totalCount
);

export const selectTasksTotalPages = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.totalPages
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

export const selectTaskEditLoading = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.editState.loading
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
