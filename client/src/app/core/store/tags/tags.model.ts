import { Tag } from '@core/models/tag';
import { ActionState, DEFAULT_ACTION_STATE } from '@core/types/action-state';
import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<Tag>();

export const initialState: TagsState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
  loadingNewTag: false,
  deleteState: DEFAULT_ACTION_STATE,
  editState: DEFAULT_ACTION_STATE,
  selectedTags: [],
});

export interface TagsState extends AsyncEntityState<Tag> {
  loading: boolean;
  loaded: boolean;
  loadingCreate: boolean;
  loadingNewTag: boolean;
  deleteState: ActionState;
  editState: ActionState;
  selectedTag?: Tag;
  createdTag?: Tag;
  selectedTags: string[];
}
