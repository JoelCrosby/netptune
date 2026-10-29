import { HttpClient } from '@angular/common/http';
import { Service, inject, signal } from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { SessionService } from '@core/services/session.service';
import { onChange } from '@core/util/signals';
import { ClientResponse } from '@core/models/client-response';
import { Subscription, catchError, finalize, of, tap } from 'rxjs';

export interface RecentItem {
  title: string;
  url: string;
  type: string;
  entityId?: string | null;
  lastAccessedAt?: string;
}

interface RecentItemsResponse {
  scope: 'workspace' | 'global';
  items: RecentItem[];
}

type RecentItemsClientResponse = ClientResponse<RecentItemsResponse>;

@Service()
export class RecentItemsService {
  private http = inject(HttpClient);

  private readonly authenticated = inject(SessionService).isAuthenticated;

  readonly items = signal<RecentItem[]>([]);
  readonly scope = signal<'workspace' | 'global'>('workspace');
  readonly loaded = signal(false);
  readonly loading = signal(false);

  private pendingLoad?: Subscription;

  constructor() {
    onChange(inject(CurrentWorkspaceService).id, () => this.invalidate());
  }

  ensureLoaded() {
    if (!this.authenticated()) return;
    if (this.loaded() || this.loading()) return;

    this.loading.set(true);

    this.pendingLoad = this.http
      .get<RecentItemsClientResponse>('api/command-palette/recent')
      .pipe(
        tap((response) => this.applyResponse(response)),
        catchError(() => of(null)),
        finalize(() => this.loading.set(false))
      )
      .subscribe(() => this.loaded.set(true));
  }

  addRecent(item: RecentItem) {
    if (!this.authenticated()) return;

    this.http
      .post<RecentItemsClientResponse>('api/command-palette/recent', item)
      .pipe(
        tap((response) => this.applyResponse(response)),
        catchError(() => of(null))
      )
      .subscribe();
  }

  clearRecent() {
    this.http
      .delete<RecentItemsClientResponse>('api/command-palette/recent')
      .pipe(
        tap((response) => this.applyResponse(response)),
        catchError(() => of(null))
      )
      .subscribe();
  }

  invalidate() {
    this.pendingLoad?.unsubscribe();
    this.loaded.set(false);
    this.items.set([]);
  }

  private applyResponse(response: RecentItemsClientResponse | null) {
    if (!response?.payload) return;

    this.scope.set(response.payload.scope);
    this.items.set(response.payload.items);
  }
}
