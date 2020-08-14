import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './layout.actions';
import { MatDrawerMode } from '@angular/material/sidenav';

const initialState: LayoutState = {
  sideMenuOpen: false,
  isMobileView: false,
  sideNavMode: 'side' as MatDrawerMode,
};

export interface LayoutState {
  sideMenuOpen: boolean;
  isMobileView: boolean;
  sideNavMode: MatDrawerMode;
}

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),

  on(actions.toggleSideMenu, (state) => ({
    ...state,
    sideMenuOpen: !state.sideMenuOpen,
  })),
  on(actions.openSideMenu, (state) => ({
    ...state,
    sideMenuOpen: true,
  })),
  on(actions.closeSideMenu, (state) => ({
    ...state,
    sideMenuOpen: false,
  })),

  on(actions.setIsMobileView, (state, { isMobileView }) => ({
    ...state,
    isMobileView,
    sideNavMode: (isMobileView ? 'over' : 'side') as MatDrawerMode,
  }))
);

export function layoutReducer(
  state: LayoutState | undefined,
  action: Action
): LayoutState {
  return reducer(state, action);
}
