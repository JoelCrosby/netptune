import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[Layout] Clear State');

// Side Menu

export const openSideMenu = createAction('[Layout] Open Side Menu');

export const closeSideMenu = createAction('[Layout] Close Side Menu');

export const toggleSideMenu = createAction('[Layout] Toggle Side Menu');

// Mobile View

export const setIsMobileView = createAction(
  '[Layout] Set Is Mobile View',
  props<{ isMobileView: boolean }>()
);
