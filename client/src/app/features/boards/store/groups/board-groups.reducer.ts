import { hubAction } from '@core/hubs/hub.utils';
import { BoardGroup } from '@core/models/board-group';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { Update } from '@ngrx/entity';
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
  on(actions.clearState, () => initialState),

  // Load Board Groups

  on(actions.loadBoardGroups, (state) => ({ ...state, loading: true })),
  on(actions.loadBoardGroupsFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(
    actions.loadBoardGroupsSuccess,
    (state, { boardGroups, selectedIds, onlyFlagged }) => {
      const selectedIdMap = new Set(selectedIds);

      return adapter.setAll(boardGroups.groups, {
        ...state,
        loading: false,
        loaded: true,
        board: boardGroups.board,
        users: boardGroups.users,
        onlyFlagged,
        selectedTasks: [],
        selectedUsers: boardGroups.users.filter((user) =>
          selectedIdMap.has(user.id)
        ),
      });
    }
  ),

  // Create Board Group

  on(actions.createBoardGroup, (state) => ({ ...state, loading: true })),
  on(actions.createBoardGroupFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.createBoardGroupSuccess, (state, { boardGroup }) =>
    adapter.addOne(boardGroup, {
      ...state,
      loadingCreate: false,
    })
  ),

  // Select Board Group

  on(actions.selectBoardGroup, (state, { boardGroup }) => ({
    ...state,
    currentBoardGroup: boardGroup,
  })),

  // Edit Board Group

  on(actions.editBoardGroup, (state, { boardGroup }) => {
    const update: Update<BoardGroup> = {
      id: boardGroup.id,
      changes: boardGroup,
    };

    return adapter.updateOne(update, state);
  }),

  // Delete Board Group

  on(actions.deleteBoardGroup, (state) => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteBoardGroupFail, (state, { error }) => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteBoardGroupSuccess, (state, { boardGroupId }) =>
    adapter.removeOne(boardGroupId, {
      ...state,
      deleteState: { loading: false },
    })
  ),

  // Move Board Group

  on(
    actions.moveTaskInBoardGroup,
    hubAction(actions.moveTaskInBoardGroup),
    (state, { request }) => moveTaskInBoardGroup(state, request)
  ),
  on(actions.setIsDragging, (state, { isDragging }) => ({
    ...state,
    isDragging,
  })),

  // Set Inline Active

  on(actions.setInlineActive, (state, { groupId }) => ({
    ...state,
    inlineActive: groupId,
  })),
  on(actions.clearInlineActive, (state) => ({
    ...state,
    inlineActive: undefined,
  })),

  // Toggle User Selection

  on(actions.toggleUserSelection, (state, { user }) => {
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

  on(actions.toggleOnlyFlagged, (state) => ({
    ...state,
    onlyFlagged: !state.onlyFlagged,
  })),

  // Select Task

  on(actions.selectTask, (state, { id }) => ({
    ...state,
    selectedTasks: (() => {
      const set = new Set(state.selectedTasks);
      const mod = set.add(id);

      return Array.from(mod);
    })(),
  })),

  // Select Task Bulk

  on(actions.selectTaskBulk, (state, { id, groupId }) => ({
    ...state,
    selectedTasks: (() => {
      const selections = getBulkTaskSelection(state, id, groupId);
      return Array.from(new Set([...state.selectedTasks, ...selections]));
    })(),
  })),

  // Deselect Task

  on(actions.deSelectTask, (state, { id }) => ({
    ...state,
    selectedTasks: state.selectedTasks.filter((t) => t !== id),
  })),

  // Deselect Task Bulk

  on(actions.deSelectTaskBulk, (state, { id, groupId }) => ({
    ...state,
    selectedTasks: (() => {
      const selections = getBulkTaskSelection(state, id, groupId);
      const selectionSet = new Set(selections);
      return state.selectedTasks.filter((task) => !selectionSet.has(task));
    })(),
  })),

  // Clear Task Selection

  on(actions.clearTaskSelection, (state) => ({
    ...state,
    selectedTasks: [],
  })),

  // ProjectTaskActions

  on(TaskActions.editProjectTask, (state, { task }) => updateTask(state, task))
);

export function boardGroupsReducer(
  state: BoardGroupsState | undefined,
  action: Action
): BoardGroupsState {
  return reducer(state, action);
}
