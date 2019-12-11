import { createSelector, createFeatureSelector } from '@ngrx/store';
import { CoreState } from './core.model';

export const selectCoreFeature = createFeatureSelector<CoreState>('core');

export const SelectCurrentWorkspace = createSelector(
  selectCoreFeature,
  (state: CoreState) => state.currentWorksapce
);

export const SelectCurrentProject = createSelector(
  selectCoreFeature,
  (state: CoreState) => state.currentProject
);
