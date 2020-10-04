import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { AppState } from '@core/core.state';
import { createFeatureSelector, createSelector } from '@ngrx/store';
import { adapter, BoardGroupsState } from './board-groups.model';
import { AppUser } from '@core/models/appuser';
import { Selected } from '@core/models/selected';
import { BoardGroup } from '@core/models/board-group';

export interface State extends AppState {
  boardgroups: BoardGroupsState;
}

const selectBoardGroupsFeature = createFeatureSelector<State, BoardGroupsState>(
  'boardgroups'
);

const { selectEntities, selectAll } = adapter.getSelectors();

export const selectAllBoardGroups = createSelector(
  selectBoardGroupsFeature,
  selectAll
);

export const selectSelectedTasks = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.selectedTasks
);

export const selectSelectedTasksCount = createSelector(
  selectSelectedTasks,
  (state: number[]) => state.length
);

export const selectAllBoardGroupsWithSelection = createSelector(
  selectAllBoardGroups,
  selectSelectedTasks,
  (state: BoardGroup[], selected: number[]) =>
    state.map((g) => ({
      ...g,
      tasks: g.tasks.map((t) => ({ ...t, selected: selected.includes(t.id) })),
    }))
);

export const selectBoardGroupEntities = createSelector(
  selectBoardGroupsFeature,
  selectEntities
);

export const selectBoardGroupsLoading = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.loading && !state.loaded
);

export const selectBoardGroupsLoaded = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.loaded
);

export const selectCurrentBoardGroup = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.currentBoardGroup
);

export const selectIsDragging = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.isDragging
);

export const selectIsInlineActive = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState, props: { groupId: number }) =>
    props.groupId === state.inlineActive
);

export const selectBoard = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state?.board
);

export const selectBoardIdentifier = createSelector(
  selectBoard,
  (state: BoardViewModel) => state?.identifier
);

export const selectBoardId = createSelector(
  selectBoard,
  (state: BoardViewModel) => state.id
);

export const selectBoardProject = createSelector(
  selectBoard,
  (state: BoardViewModel) => state.project
);

export const selectBoardGroupUsers = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.users
);

export const selectBoardGroupsSelectedUsers = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.selectedUsers
);

export const selectBoardGroupsSelectedUserIds = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.selectedUsers.map((user) => user.id)
);

export const selectBoardGroupsUsersModel = createSelector(
  selectBoardGroupUsers,
  selectBoardGroupsSelectedUsers,
  (users: AppUser[], selectedUsers: AppUser[]): Selected<AppUser>[] => {
    const selectedUserIds = new Set(selectedUsers.map((user) => user.id));

    return users.map((user) => ({
      ...user,
      selected: selectedUserIds.has(user.id),
    }));
  }
);

export const selectBoardProjectId = createSelector(
  selectBoard,
  (state: BoardViewModel) => state.projectId
);

export const selectOnlyFlagged = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.onlyFlagged
);
