import { Action, createReducer, on } from '@ngrx/store';
import { SprintStatus } from '@core/enums/sprint-status';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import * as actions from './sprints.actions';
import { adapter, initialState, SprintsState } from './sprints.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): SprintsState => initialState),
  on(actions.loadSprints.init, (state, { filter }): SprintsState => ({
    ...state,
    loading: true,
    filter: filter ?? {},
  })),
  on(actions.loadSprints.success, (state, { sprints, filter }): SprintsState =>
    adapter.setAll(sprints, {
      ...state,
      loading: false,
      loaded: true,
      filter: filter ?? state.filter,
    })
  ),
  on(actions.loadSprints.fail, (state, { error }): SprintsState => ({
    ...state,
    loading: false,
    loadingError: error,
  })),
  on(actions.loadCurrentSprints.init, (state): SprintsState => ({
    ...state,
    currentSprintsLoading: true,
  })),
  on(
    actions.loadCurrentSprints.success,
    (state, { sprints }): SprintsState => ({
      ...state,
      currentSprints: sprints,
      currentSprintsLoading: false,
      currentSprintsLoaded: true,
    })
  ),
  on(actions.loadCurrentSprints.fail, (state, { error }): SprintsState => ({
    ...state,
    loadingError: error,
    currentSprintsLoading: false,
    currentSprintsLoaded: state.currentSprintsLoaded,
  })),
  on(actions.setSprintTaskFilter, (state, { sprintId }): SprintsState => ({
    ...state,
    selectedSprintFilterId: sprintId,
  })),
  on(actions.loadSprintDetail.init, (state): SprintsState => ({
    ...state,
    detailLoading: true,
  })),
  on(actions.loadSprintDetail.success, (state, { sprint }): SprintsState =>
    adapter.upsertOne(sprint, {
      ...state,
      detail: sprint,
      detailLoading: false,
      updateState: { loading: false },
    })
  ),
  on(actions.loadSprintDetail.fail, (state, { error }): SprintsState => ({
    ...state,
    loadingError: error,
    detailLoading: false,
  })),
  on(actions.createSprint.init, (state): SprintsState => ({
    ...state,
    createState: { loading: true },
  })),
  on(actions.createSprint.success, (state, { sprint }): SprintsState =>
    adapter.addOne(sprint, {
      ...state,
      currentSprints: upsertCurrentSprint(state.currentSprints, sprint),
      createState: { loading: false },
    })
  ),
  on(actions.createSprint.fail, (state, { error }): SprintsState => ({
    ...state,
    createState: { loading: false, error },
  })),
  on(
    actions.updateSprint.init,
    actions.startSprint,
    actions.completeSprint,
    actions.addTasksToSprint,
    actions.removeTaskFromSprint,
    (state): SprintsState => ({
      ...state,
      updateState: { loading: true },
    })
  ),
  on(actions.updateSprint.success, (state, { sprint }): SprintsState => {
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
  on(actions.updateSprint.fail, (state, { error }): SprintsState => ({
    ...state,
    updateState: { loading: false, error },
  })),
  on(actions.deleteSprint.init, (state): SprintsState => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteSprint.success, (state, { sprintId }): SprintsState =>
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
  on(actions.deleteSprint.fail, (state, { error }): SprintsState => ({
    ...state,
    deleteState: { loading: false, error },
  }))
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
