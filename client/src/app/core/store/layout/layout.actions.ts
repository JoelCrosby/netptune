import { createAction, props } from '@ngrx/store';

export const initLayout = createAction('[Layout] Init');

export const clearState = createAction('[Layout] Clear State');

// Side Menu

export const openSideMenu = createAction('[Layout] Open Side Menu');

export const closeSideMenu = createAction('[Layout] Close Side Menu');

export const toggleSideMenu = createAction('[Layout] Toggle Side Menu');

// Side Nav

export const openSideNav = createAction('[Layout] Open Side Nav');

export const closeSideNav = createAction('[Layout] Close Side Nav');

export const toggleSideNav = createAction('[Layout] Toggle Side Nav');

// Mobile View

export const setIsMobileView = createAction(
  '[Layout] Set Is Mobile View',
  props<{ isMobileView: boolean }>()
);
