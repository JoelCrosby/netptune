import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './tags.actions';
import { adapter, initialState, TagsState } from './tags.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): TagsState => initialState),

  // Load Tags

  on(actions.loadTags.init, (state): TagsState => ({
    ...state,
    loaded: false,
    loading: true,
  })),
  on(actions.loadTags.fail, (state, { error }): TagsState => ({
    ...state,
    loading: false,
    loaded: false,
    loadingError: error,
  })),
  on(actions.loadTags.success, (state, { tags }): TagsState =>
    adapter.setAll(tags, {
      ...state,
      loading: false,
      loaded: true,
    })
  ),

  on(actions.editTag.success, (state, { tag }): TagsState =>
    adapter.updateOne({ id: tag.id, changes: tag }, state)
  ),

  on(actions.addTag.success, (state, { tag }): TagsState =>
    adapter.addOne(tag, state)
  )
);

export const tagsReducer = (
  state: TagsState | undefined,
  action: Action
): TagsState => reducer(state, action);
