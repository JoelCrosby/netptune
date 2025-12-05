import { RouterReducerState } from '@ngrx/router-store';
import { createFeatureSelector, createSelector } from '@ngrx/store';
import { RouterStateUrl } from './router/router.state';

export const selectRouterState =
  createFeatureSelector<RouterReducerState<RouterStateUrl>>('router');

export const selectRouterReducerState = createSelector(
  selectRouterState,
  (state: RouterReducerState<RouterStateUrl>) => state?.state
);

export const selectPageTitle = createSelector(
  selectRouterReducerState,
  (state: RouterStateUrl) => state?.title
);

export const selectRouterParam = (props: string) =>
  createSelector(
    selectRouterReducerState,
    (state: RouterStateUrl) => state?.params[props]
  );

export const selectRouterStateUrl = createSelector(
  selectRouterReducerState,
  (state: RouterStateUrl) => state.url
);

export const selectIsBoardGroupsRoute = createSelector(
  selectRouterStateUrl,
  (state: string) => {
    console.log('router-url: ', state);

    const match = state.match(/.+\/boards\/.+/) ?? [];
    return match.length > 0;
  }
);

export const selectSideBarTransparent = createSelector(
  selectRouterReducerState,
  (state: RouterStateUrl) => state?.transparentSidebar
);
