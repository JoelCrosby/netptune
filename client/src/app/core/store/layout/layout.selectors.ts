import { createSelector } from '@ngrx/store';
import { selectLayoutFeature } from '@core/core.state';
import { LayoutState } from './layout.reducer';

export const selectSideNavOpen = createSelector(
  selectLayoutFeature,
  (state: LayoutState) => state.sideNavOpen
);

export const selectSideMenuOpen = createSelector(
  selectLayoutFeature,
  (state: LayoutState) => state.sideMenuOpen
);

export const selectIsMobileView = createSelector(
  selectLayoutFeature,
  (state: LayoutState) => state.isMobileView
);

export const selectSideMenuMode = createSelector(
  selectLayoutFeature,
  (state: LayoutState) => state.sideNavMode
);
