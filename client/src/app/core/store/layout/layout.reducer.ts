import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './layout.actions';
import { MatDrawerMode } from '@angular/material/sidenav';

const initialState: LayoutState = {
  sideNavOpen: false,
  sideMenuOpen: true,
  isMobileView: false,
  sideNavMode: 'side' as MatDrawerMode,
};

export interface LayoutState {
  sideNavOpen: boolean;
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
    sideNavOpen: !state.sideMenuOpen,
  })),
  on(actions.openSideMenu, (state) => ({
    ...state,
    sideMenuOpen: true,
    sideNavOpen: true,
  })),
  on(actions.closeSideMenu, (state) => ({
    ...state,
    sideMenuOpen: false,
    sideNavOpen: false,
  })),

  on(actions.toggleSideNav, (state) => ({
    ...state,
    sideNavOpen: !state.sideNavOpen,
  })),
  on(actions.openSideNav, (state) => ({
    ...state,
    sideNavOpen: true,
  })),
  on(actions.closeSideNav, (state) => ({
    ...state,
    sideNavOpen: false,
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
