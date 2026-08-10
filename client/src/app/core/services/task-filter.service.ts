import { effect, inject, Service, signal, untracked } from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { CurrentRouteService } from '@core/router/current-route.service';
import {
  buildTaskFilterRouteParams,
  parseTaskFilterRouteParams,
  TaskFilterRouteParams,
} from '@core/router/task-filter-route-params';

const FILTER_PARAMS = ['term', 'tags', 'users', 'statusIds', 'sprintId'];

@Service()
export class TaskFilterService {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly held = signal<TaskFilterRouteParams>({});

  readonly filters = this.held.asReadonly();

  private readonly params = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });

  private readonly isFilterableRoute =
    inject(CurrentRouteService).isTaskFilterableRoute;

  private readonly workspaceId = inject(CurrentWorkspaceService).slug;

  constructor() {
    this.forgetOnWorkspaceChange();
    this.followRoute();
  }

  update(patch: TaskFilterRouteParams) {
    const next = { ...untracked(this.held), ...patch };

    this.held.set(next);
    this.writeToRoute(next);
  }

  clear() {
    const held = untracked(this.held);

    this.update({
      term: null,
      tags: [],
      users: [],
      statuses: [],
      sprintId: held.sprintId,
    });
  }

  private forgetOnWorkspaceChange() {
    let current = untracked(this.workspaceId);

    effect(() => {
      const workspace = this.workspaceId();

      if (workspace === current) return;

      current = workspace;

      untracked(() => this.held.set({}));
    });
  }

  private followRoute() {
    effect(() => {
      const params = this.params();
      const isFilterable = this.isFilterableRoute();

      if (!isFilterable) return;

      untracked(() => {
        // A link describes the whole filter, so arriving on one must not inherit
        // half of the previous view's.
        const isLinked = FILTER_PARAMS.some((param) => params.has(param));

        if (isLinked) {
          this.held.set(parseTaskFilterRouteParams(params));

          return;
        }

        const held = this.held();

        if (!this.hasAny(held)) return;

        this.writeToRoute(held);
      });
    });
  }

  private hasAny(filters: TaskFilterRouteParams): boolean {
    return (
      !!filters.term ||
      !!filters.tags?.length ||
      !!filters.users?.length ||
      !!filters.statuses?.length ||
      filters.sprintId !== undefined
    );
  }

  private writeToRoute(filters: TaskFilterRouteParams) {
    const queryParams: Params = {
      term: null,
      tags: null,
      users: null,
      statusIds: null,
      sprintId: null,
      ...buildTaskFilterRouteParams(filters, { includeStatuses: true }),
    };

    void this.router.navigate([], {
      queryParams,
      queryParamsHandling: 'merge',
    });
  }
}
