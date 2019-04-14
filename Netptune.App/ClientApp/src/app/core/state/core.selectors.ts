import { createSelector, createFeatureSelector } from '@ngrx/store';
import { CoreState } from './core.reducer';

export const selectCoreFeature = createFeatureSelector<CoreState>('core');

export const SelectCurrentWorkspace = createSelector(
  selectCoreFeature,
  (state: CoreState) => state.currentWorksapce
);
