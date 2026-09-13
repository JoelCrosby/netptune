import { TaskFilterRouteParams } from '@core/router/task-filter-route-params';

export function appendTaskFilters(
  query: URLSearchParams,
  filters: TaskFilterRouteParams
): void {
  if (filters.term) {
    query.set('search', filters.term);
  }

  filters.users?.forEach((value) => query.append('assignees', value));
  filters.tags?.forEach((value) => query.append('tags', value));
  filters.statuses?.forEach((value) => {
    query.append('statusIds', value.toString());
  });
}
