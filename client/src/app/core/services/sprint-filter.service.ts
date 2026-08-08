import { effect, inject, Injectable, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import * as RouteSelectors from '@core/core.route.selectors';
import { parseTaskFilterRouteParams } from '@core/router/task-filter-route-params';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';

/**
 * The sprint a task view is narrowed to. Unlike the other task filters this one
 * follows the user between views, so it is held here and written back into the URL
 * of every filterable route — which is also what makes a filtered view linkable,
 * and what makes the views reload, since they all reload on navigation.
 */
@Injectable({ providedIn: 'root' })
export class SprintFilterService {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly selected = signal<number | undefined>(undefined);

  readonly sprintId = this.selected.asReadonly();

  private readonly params = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });

  private readonly isFilterableRoute = this.store.selectSignal(
    RouteSelectors.selectIsSprintFilterableRoute
  );

  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  constructor() {
    this.forgetOnWorkspaceChange();
    this.followRoute();
  }

  set(sprintId?: number) {
    this.selected.set(sprintId);
    this.writeToRoute(sprintId);
  }

  clear() {
    this.set(undefined);
  }

  /** A sprint that was deleted, or is no longer active, cannot stay the filter. */
  clearIfSelected(sprintId: number) {
    if (untracked(this.selected) !== sprintId) return;

    this.clear();
  }

  private forgetOnWorkspaceChange() {
    let current = untracked(this.workspaceId);

    effect(() => {
      const workspace = this.workspaceId();

      if (workspace === current) return;

      current = workspace;

      untracked(() => this.selected.set(undefined));
    });
  }

  /* A link carrying a sprint wins; a filterable route without one inherits the held sprint. */
  private followRoute() {
    effect(() => {
      const params = this.params();
      const isFilterable = this.isFilterableRoute();

      if (!isFilterable) return;

      untracked(() => {
        if (params.has('sprintId')) {
          this.selected.set(parseTaskFilterRouteParams(params).sprintId);

          return;
        }

        const held = this.selected();

        if (held === undefined) return;

        this.writeToRoute(held);
      });
    });
  }

  private writeToRoute(sprintId?: number) {
    void this.router.navigate([], {
      queryParams: { sprintId: sprintId ?? null },
      queryParamsHandling: 'merge',
    });
  }
}
