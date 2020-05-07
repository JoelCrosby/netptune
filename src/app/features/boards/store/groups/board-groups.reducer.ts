import { MoveTaskInGroupRequest } from '@app/core/models/move-task-in-group-request';
import { getNewSortOrder } from '@app/core/util/sort-order-helper';
import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './board-groups.actions';
import { adapter, BoardGroupsState, initialState } from './board-groups.model';
import { moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';

const moveTaskInBoardGroup = (
  state: BoardGroupsState,
  request: MoveTaskInGroupRequest
): BoardGroupsState => {
  if (request.oldGroupId === request.newGroupId) {
    moveItemInArray(
      state.entities[request.newGroupId].tasks,
      request.previousIndex,
      request.currentIndex
    );
  } else {
    transferArrayItem(
      state.entities[request.oldGroupId].tasks,
      state.entities[request.newGroupId].tasks,
      request.previousIndex,
      request.currentIndex
    );
  }

  const groups = request.tasks;

  const prevGroup = groups[request.currentIndex - 1];
  const nextGroup = groups[request.currentIndex + 1];

  const preOrder = prevGroup?.sortOrder;
  const nextOrder = nextGroup?.sortOrder;

  const sortOrder = getNewSortOrder(preOrder, nextOrder);

  state.entities[request.newGroupId].tasks = state.entities[
    request.newGroupId
  ].tasks.map((task) => {
    if (task.id !== request.taskId) return task;

    return { ...task, sortOrder };
  });

  return state;
};

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
  )
);

export function boardGroupsReducer(
  state: BoardGroupsState | undefined,
  action: Action
) {
  return reducer(state, action);
}
