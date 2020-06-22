import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './board-groups.actions';
import * as TaskActions from '@project-tasks/store/tasks.actions';
import { adapter, BoardGroupsState, initialState } from './board-groups.model';
import { moveTaskInBoardGroup, updateTask } from './board-group.utils';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),
  on(actions.loadBoardGroups, (state) => ({ ...state, loading: true })),
  on(actions.loadBoardGroupsFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.loadBoardGroupsSuccess, (state, { boardGroups }) =>
    adapter.setAll(boardGroups, { ...state, loading: false, loaded: true })
  ),
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
  on(actions.selectBoardGroup, (state, { boardGroup }) => ({
    ...state,
    currentBoardGroup: boardGroup,
  })),
  on(actions.deleteBoardGroup, (state) => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteBoardGroupFail, (state, { error }) => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteBoardGroupSuccess, (state, { boardGroup }) =>
    adapter.removeOne(boardGroup.id, {
      ...state,
      deleteState: { loading: false },
    })
  ),
  on(actions.moveTaskInBoardGroup, (state, { request }) =>
    moveTaskInBoardGroup(state, request)
  ),
  on(actions.setIsDragging, (state, { isDragging }) => ({
    ...state,
    isDragging,
  })),
  on(actions.setInlineActive, (state, { groupId }) => ({
    ...state,
    inlineActive: groupId,
  })),
  on(actions.clearInlineActive, (state) => ({
    ...state,
    inlineActive: undefined,
  })),
  on(TaskActions.editProjectTask, (state, { task }) => updateTask(state, task))

  // ProjectTaskActions
);

export function boardGroupsReducer(
  state: BoardGroupsState | undefined,
  action: Action
) {
  return reducer(state, action);
}
