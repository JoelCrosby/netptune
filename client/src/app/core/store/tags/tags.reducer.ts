import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './tags.actions';
import { adapter, initialState, TagsState } from './tags.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): TagsState => initialState),

  // Load Tags

  on(
    actions.loadTags,
    (state): TagsState => ({ ...state, loaded: false, loading: true })
  ),
  on(
    actions.loadTagsFail,
    (state, { error }): TagsState => ({
      ...state,
      loading: false,
      loaded: false,
      loadingError: error,
    })
  ),
  on(
    actions.loadTagsSuccess,
    (state, { tags }): TagsState =>
      adapter.setAll(tags, {
        ...state,
        loading: false,
        loaded: true,
      })
  ),

  on(
    actions.toggleSelectedTag,
    (state, { tag }): TagsState => ({
      ...state,
      selectedTags: state.selectedTags.includes(tag)
        ? state.selectedTags.filter((t) => t !== tag)
        : Array.from(new Set([...state.selectedTags, tag])),
    })
  ),

  on(
    actions.editTagSuccess,
    (state, { tag }): TagsState =>
      adapter.updateOne({ id: tag.id, changes: tag }, state)
  ),

  on(
    actions.addTagSuccess,
    (state, { tag }): TagsState => adapter.addOne(tag, state)
  ),

  on(
    actions.setSelectedTags,
    (state, { selectedTags }): TagsState => ({
      ...state,
      selectedTags,
    })
  )
);

export const tagsReducer = (
  state: TagsState | undefined,
  action: Action
): TagsState => reducer(state, action);
