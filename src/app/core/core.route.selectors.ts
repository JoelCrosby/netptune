import { RouterReducerState } from '@ngrx/router-store';
import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AppState } from './core.state';
import { RouterStateUrl } from './router/router.state';

export const selectRouterState = createFeatureSelector<
  AppState,
  RouterReducerState<RouterStateUrl>
>('router');

export const selectRouterReducerState = createSelector(
  selectRouterState,
  (state: RouterReducerState<RouterStateUrl>) => state?.state
);

export const selectPageTitle = createSelector(
  selectRouterReducerState,
  (state: RouterStateUrl) => state?.title
);

export const selectRouterParam = createSelector(
  selectRouterReducerState,
  (state: RouterStateUrl, props: string) => state?.params[props]
);

export const selectRouterStateUrl = createSelector(
  selectRouterReducerState,
  (state: RouterStateUrl) => state.url
);

export const isBoardGroupsRoute = createSelector(
  selectPageTitle,
  (state: string) => state === 'Boards'
);

export const selectSideBarTransparent = createSelector(
  selectRouterReducerState,
  (state: RouterStateUrl) => state?.transparentSidebar
);
