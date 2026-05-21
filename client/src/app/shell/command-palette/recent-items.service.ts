import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { catchError, finalize, of, tap } from 'rxjs';

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

@Injectable({ providedIn: 'root' })
export class RecentItemsService {
  private http = inject(HttpClient);

  readonly items = signal<RecentItem[]>([]);
  readonly scope = signal<'workspace' | 'global'>('workspace');
  readonly loaded = signal(false);
  readonly loading = signal(false);

  ensureLoaded() {
    if (this.loaded() || this.loading()) return;

    this.loading.set(true);

    this.http
      .get<RecentItemsClientResponse>('api/command-palette/recent')
      .pipe(
        tap((response) => this.applyResponse(response)),
        catchError(() => of(null)),
        finalize(() => this.loading.set(false))
      )
      .subscribe(() => this.loaded.set(true));
  }

  addRecent(item: RecentItem) {
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
    this.loaded.set(false);
    this.items.set([]);
  }

  private applyResponse(response: RecentItemsClientResponse | null) {
    if (!response?.payload) return;

    this.scope.set(response.payload.scope);
    this.items.set(response.payload.items);
  }
}
