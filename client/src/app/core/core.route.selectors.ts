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

export const selectRouterStateUrl = createSelector(
  selectRouterReducerState,
  (state: RouterStateUrl) => state.url
);

export const selectIsBoardGroupsRoute = createSelector(
  selectRouterStateUrl,
  (state: string) => {
    const match = state.match(/\/.+\/boards\/.+/) ?? [];
    return match.length > 0;
  }
);

export const selectIsTasksRoute = createSelector(
  selectRouterStateUrl,
  (state: string) => {
    const match = state.match(/\/.+\/tasks/) ?? [];
    return match.length > 0;
  }
);

export const selectIsTaskListRoute = createSelector(
  selectRouterStateUrl,
  (state: string) => /^\/[^/?#]+\/tasks(?:[?#].*)?$/.test(state ?? '')
);

export const selectIsSprintFilterableRoute = createSelector(
  selectIsBoardGroupsRoute,
  selectIsTaskListRoute,
  (isBoardGroupsRoute, isTaskListRoute) => {
    return isBoardGroupsRoute || isTaskListRoute;
  }
);

export const selectIsCalendarRoute = createSelector(
  selectRouterStateUrl,
  (state: string) => /^\/[^/?#]+\/calendar(?:[?#].*)?$/.test(state ?? '')
);

export const selectIsRoadmapRoute = createSelector(
  selectRouterStateUrl,
  (state: string) => /^\/[^/?#]+\/roadmap(?:[?#].*)?$/.test(state ?? '')
);

export const selectIsSprintBacklogRoute = createSelector(
  selectRouterStateUrl,
  (state: string) =>
    /^\/[^/?#]+\/sprints\/backlog(?:[?#].*)?$/.test(state ?? '')
);

/** Every view whose task filters are shared, so the filters follow the user between them. */
export const selectIsTaskFilterableRoute = createSelector(
  selectIsSprintFilterableRoute,
  selectIsSprintBacklogRoute,
  selectIsCalendarRoute,
  selectIsRoadmapRoute,
  (isSprintFilterable, isBacklog, isCalendar, isRoadmap) =>
    isSprintFilterable || isBacklog || isCalendar || isRoadmap
);

export const selectSideBarTransparent = createSelector(
  selectRouterReducerState,
  (state: RouterStateUrl) => state?.transparentSidebar
);
