import {
  computed,
  effect,
  inject,
  Injectable,
  linkedSignal,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { AppUser } from '@core/models/appuser';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { Selected } from '@core/models/selected';
import { Status } from '@core/models/status';
import { boardViewResource } from '@core/resources/board-view.resource';
import { buildTaskFilterRouteParams } from '@core/router/task-filter-route-params';
import { TaskFilterService } from '@core/services/task-filter.service';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { moveTaskInGroups, sortBySortOrder } from '@core/util/board-groups';

@Injectable({ providedIn: 'root' })
export class BoardViewService {
  private readonly router = inject(Router);
  private readonly filters = inject(TaskFilterService);
  private readonly hub = inject(ProjectTasksHubService);

  private readonly openIdentifier = signal<string | undefined>(undefined);

  readonly identifier = this.openIdentifier.asReadonly();

  private readonly requestParams = computed(() => {
    return buildTaskFilterRouteParams(this.filters.filters(), {
      includeStatuses: true,
    });
  });

  private readonly resource = boardViewResource(
    this.identifier,
    this.requestParams
  );

  /** The board as the server last sent it, before any optimistic edit. */
  readonly loadedBoard = this.resource.loadedValue;

  private readonly view = this.resource.value;

  readonly board = computed(() => this.view()?.board);
  readonly users = computed(() => this.view()?.users ?? []);
  readonly loaded = computed(() => this.view() !== undefined);
  readonly loading = computed(() => {
    return this.resource.isLoading() && !this.loaded();
  });

  readonly groups = computed(() => {
    return [...(this.view()?.groups ?? [])].sort(sortBySortOrder);
  });

  private readonly dragging = linkedSignal<string | undefined, boolean>({
    source: this.identifier,
    computation: () => false,
  });

  readonly isDragging = this.dragging.asReadonly();

  readonly onlineUserIds = this.hub.onlineUserIds;

  readonly userOptions = computed<BoardGroupUserModel[]>(() => {
    const selected = new Set(this.filters.filters().users ?? []);
    const online = new Set(this.onlineUserIds());

    return this.users().map((user) => {
      return {
        ...user,
        selected: selected.has(user.id),
        online: online.has(user.id),
      };
    });
  });

  constructor() {
    this.leaveOnLoadFailure();
  }

  open(identifier: string) {
    this.openIdentifier.set(identifier);
  }

  close() {
    this.openIdentifier.set(undefined);
  }

  reload() {
    this.resource.reload();
  }

  applyTaskMove(request: MoveTaskInGroupRequest, status?: Status | null) {
    this.view.update((view) => {
      if (!view) return view;

      return {
        ...view,
        groups: moveTaskInGroups(view.groups, request, status),
      };
    });
  }

  setIsDragging(isDragging: boolean) {
    this.dragging.set(isDragging);
  }

  private leaveOnLoadFailure() {
    effect(() => {
      const loadFailed = !!this.resource.error();

      if (!loadFailed) return;

      void this.router.navigateByUrl(this.boardListUrl());
    });
  }

  private boardListUrl() {
    const segments = this.router.routerState.snapshot.url.split('/');

    segments.pop();

    return segments.join('/');
  }
}

export type BoardGroupUserModel = Selected<AppUser> & { online: boolean };
