import { hubAction } from '@core/hubs/hub.utils';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { Action, createReducer, on } from '@ngrx/store';
import {
  getBulkTaskSelection,
  moveTaskInBoardGroup,
  updateTask,
} from './board-group.utils';
import * as actions from './board-groups.actions';
import { adapter, BoardGroupsState, initialState } from './board-groups.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): BoardGroupsState => initialState),

  // Load Board Groups

  on(
    actions.loadBoardGroups,
    (state): BoardGroupsState => ({ ...state, loading: true })
  ),
  on(
    actions.loadBoardGroupsFail,
    (state, { error }): BoardGroupsState => ({
      ...state,
      loadingError: error,
    })
  ),
  on(
    actions.loadBoardGroupsSuccess,
    (
      state,
      { boardGroups, selectedIds, onlyFlagged, searchTerm }
    ): BoardGroupsState => {
      const selectedIdMap = new Set(selectedIds);

      return adapter.setAll(boardGroups.groups, {
        ...state,
        loading: false,
        loaded: true,
        board: boardGroups.board,
        users: boardGroups.users,
        onlyFlagged,
        searchTerm,
        selectedTasks: [],
        selectedUsers: boardGroups.users.filter((user) =>
          selectedIdMap.has(user.id)
        ),
      });
    }
  ),

  // Create Board Group

  on(
    actions.createBoardGroup,
    (state): BoardGroupsState => ({ ...state, loading: true })
  ),
  on(
    actions.createBoardGroupFail,
    (state, { error }): BoardGroupsState => ({
      ...state,
      loadingError: error,
    })
  ),

  // Select Board Group

  on(
    actions.selectBoardGroup,
    (state, { boardGroup }): BoardGroupsState => ({
      ...state,
      currentBoardGroup: boardGroup,
    })
  ),

  // Delete Board Group

  on(
    actions.deleteBoardGroup,
    (state): BoardGroupsState => ({
      ...state,
      deleteState: { loading: true },
    })
  ),
  on(
    actions.deleteBoardGroupFail,
    (state, { error }): BoardGroupsState => ({
      ...state,
      deleteState: { loading: false, error },
    })
  ),
  on(
    actions.deleteBoardGroupSuccess,
    (state, { boardGroupId }): BoardGroupsState =>
      adapter.removeOne(boardGroupId, {
        ...state,
        deleteState: { loading: false },
      })
  ),

  // Move Board Group

  on(
    actions.moveTaskInBoardGroup,
    hubAction(actions.moveTaskInBoardGroup),
    (state, { request }): BoardGroupsState =>
      moveTaskInBoardGroup(state, request)
  ),
  on(
    actions.setIsDragging,
    (state, { isDragging }): BoardGroupsState => ({
      ...state,
      isDragging,
    })
  ),

  // Set Inline Active

  on(
    actions.setInlineActive,
    (state, { groupId }): BoardGroupsState => ({
      ...state,
      inlineActive: groupId,
    })
  ),
  on(
    actions.clearInlineActive,
    (state): BoardGroupsState => ({
      ...state,
      inlineActive: undefined,
    })
  ),

  // Toggle User Selection

  on(actions.toggleUserSelection, (state, { user }): BoardGroupsState => {
    const exists = state.selectedUsers.find((item) => item.id === user.id);

    const selectedUsers = exists
      ? state.selectedUsers.filter((item) => item.id !== user.id)
      : [...state.selectedUsers, user];

    return {
      ...state,
      selectedUsers,
    };
  }),

  // Toggle Only Flagged

  on(
    actions.toggleOnlyFlagged,
    (state): BoardGroupsState => ({
      ...state,
      onlyFlagged: !state.onlyFlagged,
    })
  ),

  // Set Search Term

  on(
    actions.setSearchTerm,
    (state, { term }): BoardGroupsState => ({
      ...state,
      searchTerm: term,
    })
  ),

  // Select Task

  on(
    actions.selectTask,
    (state, { id }): BoardGroupsState => ({
      ...state,
      selectedTasks: (() => {
        const set = new Set(state.selectedTasks);
        const mod = set.add(id);

        return Array.from(mod);
      })(),
    })
  ),

  // Select Task Bulk

  on(
    actions.selectTaskBulk,
    (state, { id, groupId }): BoardGroupsState => ({
      ...state,
      selectedTasks: (() => {
        const selections = getBulkTaskSelection(state, id, groupId);
        return Array.from(new Set([...state.selectedTasks, ...selections]));
      })(),
    })
  ),

  // Deselect Task

  on(
    actions.deSelectTask,
    (state, { id }): BoardGroupsState => ({
      ...state,
      selectedTasks: state.selectedTasks.filter((t) => t !== id),
    })
  ),

  // Deselect Task Bulk

  on(
    actions.deSelectTaskBulk,
    (state, { id, groupId }): BoardGroupsState => ({
      ...state,
      selectedTasks: (() => {
        const selections = getBulkTaskSelection(state, id, groupId);
        const selectionSet = new Set(selections);
        return state.selectedTasks.filter((task) => !selectionSet.has(task));
      })(),
    })
  ),

  // Clear Task Selection

  on(
    actions.clearTaskSelection,
    (state): BoardGroupsState => ({
      ...state,
      selectedTasks: [],
    })
  ),

  // ProjectTaskActions

  on(
    TaskActions.editProjectTask,
    (state, { task }): BoardGroupsState => updateTask(state, task)
  ),

  // Set Inline Task Content

  on(
    actions.setInlineTaskContent,
    (state, { content }): BoardGroupsState => ({
      ...state,
      inlineTaskContent: content,
    })
  ),

  // Set Inline Task Content

  on(
    actions.setIsInlineDirty,
    (state, { isDirty }): BoardGroupsState => ({
      ...state,
      isInlineDirty: isDirty,
    })
  )
);

export const boardGroupsReducer = (
  state: BoardGroupsState | undefined,
  action: Action
): BoardGroupsState => reducer(state, action);
