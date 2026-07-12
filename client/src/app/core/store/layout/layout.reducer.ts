import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './layout.actions';

const initialState: LayoutState = {
  sideMenuOpen: true,
  isMobileView: false,
};

export interface LayoutState {
  sideMenuOpen: boolean;
  isMobileView: boolean;
}

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): LayoutState => initialState),

  on(actions.toggleSideMenu, (state): LayoutState => ({
    ...state,
    sideMenuOpen: !state.sideMenuOpen,
  })),
  on(actions.openSideMenu, (state): LayoutState => ({
    ...state,
    sideMenuOpen: true,
  })),
  on(actions.closeSideMenu, (state): LayoutState => ({
    ...state,
    sideMenuOpen: false,
  })),

  on(actions.setIsMobileView, (state, { isMobileView }): LayoutState => ({
    ...state,
    isMobileView,
  }))
);

export const layoutReducer = (
  state: LayoutState | undefined,
  action: Action
): LayoutState => reducer(state, action);
