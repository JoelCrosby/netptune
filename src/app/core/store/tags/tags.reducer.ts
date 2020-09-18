import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './tags.actions';
import { adapter, initialState, TagsState } from './tags.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),

  // Load Tags

  on(actions.loadTags, (state) => ({ ...state, loading: true })),
  on(actions.loadTagsFail, (state, { error }) => ({
    ...state,
    loading: false,
    loadProjectsError: error,
  })),
  on(actions.loadTagsSuccess, (state, { tags }) =>
    adapter.setAll(tags, {
      ...state,
      loading: false,
      loaded: true,
    })
  )
);

export function tagsReducer(
  state: TagsState | undefined,
  action: Action
): TagsState {
  return reducer(state, action);
}
