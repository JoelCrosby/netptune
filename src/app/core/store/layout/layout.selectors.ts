import { createSelector } from '@ngrx/store';
import { selectLayoutState } from '@core/core.state';
import { LayoutState } from './layout.reducer';

export const selectSideMenuOpen = createSelector(
  selectLayoutState,
  (state: LayoutState) => state.sideMenuOpen
);

export const selectIsMobileView = createSelector(
  selectLayoutState,
  (state: LayoutState) => state.isMobileView
);

export const selectSideMenuMode = createSelector(
  selectLayoutState,
  (state: LayoutState) => state.sideNavMode
);
