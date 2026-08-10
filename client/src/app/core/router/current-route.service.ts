import { computed, inject, Service } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs/operators';

@Service()
export class CurrentRouteService {
  private readonly router = inject(Router);

  readonly url = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  readonly isBoardRoute = computed(() => /\/.+\/boards\/.+/.test(this.url()));

  readonly isTaskListRoute = computed(() => {
    return /^\/[^/?#]+\/tasks(?:[?#].*)?$/.test(this.url());
  });

  readonly isCalendarRoute = computed(() => {
    return /^\/[^/?#]+\/calendar(?:[?#].*)?$/.test(this.url());
  });

  readonly isRoadmapRoute = computed(() => {
    return /^\/[^/?#]+\/roadmap(?:[?#].*)?$/.test(this.url());
  });

  readonly isSprintBacklogRoute = computed(() => {
    return /^\/[^/?#]+\/sprints\/backlog(?:[?#].*)?$/.test(this.url());
  });

  readonly isSprintFilterableRoute = computed(() => {
    return this.isBoardRoute() || this.isTaskListRoute();
  });

  /** Every view whose task filters are shared, so the filters follow the user between them. */
  readonly isTaskFilterableRoute = computed(() => {
    return (
      this.isSprintFilterableRoute() ||
      this.isSprintBacklogRoute() ||
      this.isCalendarRoute() ||
      this.isRoadmapRoute()
    );
  });
}
