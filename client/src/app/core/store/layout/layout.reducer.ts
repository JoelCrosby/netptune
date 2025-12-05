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
  on(actions.clearState, (): LayoutState => initialState),

  on(
    actions.toggleSideMenu,
    (state): LayoutState => ({
      ...state,
      sideMenuOpen: !state.sideMenuOpen,
      sideNavOpen: !state.sideMenuOpen,
    })
  ),
  on(
    actions.openSideMenu,
    (state): LayoutState => ({
      ...state,
      sideMenuOpen: true,
      sideNavOpen: true,
    })
  ),
  on(
    actions.closeSideMenu,
    (state): LayoutState => ({
      ...state,
      sideMenuOpen: false,
      sideNavOpen: false,
    })
  ),

  on(
    actions.toggleSideNav,
    (state): LayoutState => ({
      ...state,
      sideNavOpen: !state.sideNavOpen,
    })
  ),
  on(
    actions.openSideNav,
    (state): LayoutState => ({
      ...state,
      sideNavOpen: true,
    })
  ),
  on(
    actions.closeSideNav,
    (state): LayoutState => ({
      ...state,
      sideNavOpen: false,
    })
  ),

  on(
    actions.setIsMobileView,
    (state, { isMobileView }): LayoutState => ({
      ...state,
      isMobileView,
      sideNavMode: (isMobileView ? 'over' : 'side') as MatDrawerMode,
    })
  )
);

export const layoutReducer = (
  state: LayoutState | undefined,
  action: Action
): LayoutState => reducer(state, action);
