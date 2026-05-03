import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './sprints.actions';
import { adapter, initialState, SprintsState } from './sprints.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): SprintsState => initialState),
  on(
    actions.loadSprints,
    (state, { filter }): SprintsState => ({
      ...state,
      loading: true,
      filter: filter ?? {},
    })
  ),
  on(
    actions.loadSprintsSuccess,
    (state, { sprints, filter }): SprintsState =>
      adapter.setAll(sprints, {
        ...state,
        loading: false,
        loaded: true,
        filter: filter ?? state.filter,
      })
  ),
  on(
    actions.loadSprintsFail,
    (state, { error }): SprintsState => ({
      ...state,
      loading: false,
      loadingError: error,
    })
  ),
  on(
    actions.loadSprintDetail,
    (state): SprintsState => ({ ...state, detailLoading: true })
  ),
  on(
    actions.loadSprintDetailSuccess,
    (state, { sprint }): SprintsState =>
      adapter.upsertOne(sprint, {
        ...state,
        detail: sprint,
        detailLoading: false,
        updateState: { loading: false },
      })
  ),
  on(
    actions.loadSprintDetailFail,
    (state, { error }): SprintsState => ({
      ...state,
      loadingError: error,
      detailLoading: false,
    })
  ),
  on(
    actions.createSprint,
    (state): SprintsState => ({
      ...state,
      createState: { loading: true },
    })
  ),
  on(
    actions.createSprintSuccess,
    (state, { sprint }): SprintsState =>
      adapter.addOne(sprint, {
        ...state,
        createState: { loading: false },
      })
  ),
  on(
    actions.createSprintFail,
    (state, { error }): SprintsState => ({
      ...state,
      createState: { loading: false, error },
    })
  ),
  on(
    actions.updateSprint,
    actions.startSprint,
    actions.completeSprint,
    actions.addTasksToSprint,
    actions.removeTaskFromSprint,
    (state): SprintsState => ({
      ...state,
      updateState: { loading: true },
    })
  ),
  on(
    actions.updateSprintSuccess,
    (state, { sprint }): SprintsState =>
      adapter.upsertOne(sprint, {
        ...state,
        updateState: { loading: false },
      })
  ),
  on(
    actions.updateSprintFail,
    (state, { error }): SprintsState => ({
      ...state,
      updateState: { loading: false, error },
    })
  ),
  on(
    actions.deleteSprint,
    (state): SprintsState => ({
      ...state,
      deleteState: { loading: true },
    })
  ),
  on(
    actions.deleteSprintSuccess,
    (state, { sprintId }): SprintsState =>
      adapter.removeOne(sprintId, {
        ...state,
        detail: state.detail?.id === sprintId ? undefined : state.detail,
        deleteState: { loading: false },
      })
  ),
  on(
    actions.deleteSprintFail,
    (state, { error }): SprintsState => ({
      ...state,
      deleteState: { loading: false, error },
    })
  )
);

export const sprintsReducer = (
  state: SprintsState | undefined,
  action: Action
): SprintsState => reducer(state, action);
