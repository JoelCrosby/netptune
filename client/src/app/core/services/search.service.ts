import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { SearchResponse, SearchResult } from '@core/models/search-result';
import { WorkspaceService } from '@core/services/workspace.service';
import { debounceTime, switchMap, catchError, of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private http = inject(HttpClient);
  private workspace = inject(WorkspaceService);
  private destroyRef = inject(DestroyRef);

  readonly query = signal('');
  readonly results = signal<SearchResult[]>([]);

  constructor() {
    toObservable(this.query)
      .pipe(
        debounceTime(400),
        switchMap((q) => {
          if (!q.trim()) {
            this.results.set([]);
            return of(null);
          }

          const slug = this.workspace.getWorkspaceRoute();

          if (!slug) return of(null);

          return this.http
            .get<SearchResponse>('api/search', {
              params: { q, workspace: slug },
            })
            .pipe(catchError(() => of(null)));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((response) => {
        if (response) {
          this.results.set(response.results);
        }
      });
  }
}
