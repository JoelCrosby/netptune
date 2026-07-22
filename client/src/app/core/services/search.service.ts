import { HttpParams, httpResource } from '@angular/common/http';
import { Injectable, computed, debounced, signal } from '@angular/core';
import { SearchResponse, SearchResult } from '@core/models/search-result';

export type SearchType = 'tasks' | 'projects' | 'boards' | 'sprints';

interface SearchRequest {
  query: string;
  types: readonly SearchType[];
}

const EMPTY_SEARCH_RESPONSE: SearchResponse = {
  results: [],
  processingTimeMs: 0,
};

@Injectable({ providedIn: 'root' })
export class SearchService {
  private readonly request = signal<SearchRequest>({ query: '', types: [] });
  private readonly debouncedRequest = debounced(this.request, 400);
  private readonly resource = httpResource<SearchResponse>(
    () => {
      const { query, types } = this.debouncedRequest.value();

      if (!query.trim()) return undefined;

      let params = new HttpParams().set('q', query);

      for (const type of types) {
        params = params.append('types', type);
      }

      return { url: 'api/search', params };
    },
    { defaultValue: EMPTY_SEARCH_RESPONSE }
  );

  readonly results = computed<SearchResult[]>(() => {
    const request = this.request();
    const settledRequest = this.debouncedRequest.value();
    const scopeChanged = !this.typesMatch(request.types, settledRequest.types);

    if (!request.query.trim() || scopeChanged || !this.resource.hasValue()) {
      return [];
    }

    return this.resource.value().results;
  });

  setQuery(query: string, types: readonly SearchType[] = []): void {
    this.request.set({ query, types });
  }

  private typesMatch(
    first: readonly SearchType[],
    second: readonly SearchType[]
  ): boolean {
    return (
      first.length === second.length &&
      first.every((type, index) => type === second[index])
    );
  }
}
