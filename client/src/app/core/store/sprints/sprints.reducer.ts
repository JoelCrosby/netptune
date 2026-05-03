import { Action, createReducer, on } from '@ngrx/store';
import { SprintStatus } from '@core/enums/sprint-status';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
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
    actions.loadCurrentSprints,
    (state): SprintsState => ({
      ...state,
      currentSprintsLoading: true,
    })
  ),
  on(
    actions.loadCurrentSprintsSuccess,
    (state, { sprints }): SprintsState => ({
      ...state,
      currentSprints: sprints,
      currentSprintsLoading: false,
      currentSprintsLoaded: true,
    })
  ),
  on(
    actions.loadCurrentSprintsFail,
    (state, { error }): SprintsState => ({
      ...state,
      loadingError: error,
      currentSprintsLoading: false,
      currentSprintsLoaded: state.currentSprintsLoaded,
    })
  ),
  on(
    actions.setSprintTaskFilter,
    (state, { sprintId }): SprintsState => ({
      ...state,
      selectedSprintFilterId: sprintId,
    })
  ),
  on(
    actions.loadSprintDetail,
    (state): SprintsState => ({
      ...state,
      detailLoading: true,
      availableTasks: [],
      availableTasksLoading: false,
    })
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
    actions.loadAvailableSprintTasks,
    (state): SprintsState => ({ ...state, availableTasksLoading: true })
  ),
  on(
    actions.loadAvailableSprintTasksSuccess,
    (state, { tasks }): SprintsState => ({
      ...state,
      availableTasks: tasks,
      availableTasksLoading: false,
    })
  ),
  on(
    actions.loadAvailableSprintTasksFail,
    (state, { error }): SprintsState => ({
      ...state,
      loadingError: error,
      availableTasksLoading: false,
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
        currentSprints: upsertCurrentSprint(state.currentSprints, sprint),
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
  on(actions.updateSprintSuccess, (state, { sprint }): SprintsState => {
    const detail =
      state.detail?.id === sprint.id
        ? { ...state.detail, ...sprint }
        : state.detail;

    return adapter.upsertOne(sprint, {
      ...state,
      detail,
      currentSprints: upsertCurrentSprint(state.currentSprints, sprint),
      selectedSprintFilterId:
        state.selectedSprintFilterId === sprint.id &&
        sprint.status !== SprintStatus.active
          ? undefined
          : state.selectedSprintFilterId,
      updateState: { loading: false },
    });
  }),
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
        currentSprints: state.currentSprints.filter(
          (sprint) => sprint.id !== sprintId
        ),
        selectedSprintFilterId:
          state.selectedSprintFilterId === sprintId
            ? undefined
            : state.selectedSprintFilterId,
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

function upsertCurrentSprint(
  currentSprints: SprintViewModel[],
  sprint: SprintViewModel
): SprintViewModel[] {
  if (sprint.status !== SprintStatus.active) {
    return currentSprints.filter(
      (currentSprint) => currentSprint.id !== sprint.id
    );
  }

  const exists = currentSprints.some(
    (currentSprint) => currentSprint.id === sprint.id
  );

  if (!exists) {
    return [...currentSprints, sprint];
  }

  return currentSprints.map((currentSprint) =>
    currentSprint.id === sprint.id ? sprint : currentSprint
  );
}
