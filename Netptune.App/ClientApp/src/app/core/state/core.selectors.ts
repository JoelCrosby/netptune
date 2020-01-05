import { createSelector, createFeatureSelector } from '@ngrx/store';
import { CoreState } from './core.model';

export const selectCoreFeature = createFeatureSelector<CoreState>('core');
