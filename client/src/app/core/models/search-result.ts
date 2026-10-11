export type SearchResultType = 'task' | 'project' | 'board' | 'sprint' | 'page';

export interface SearchResult {
  type: SearchResultType;
  id: number;
  title: string;
  subtitle: string;
  url: string;
  metadata: Record<string, unknown>;
}

export interface SearchResponse {
  results: SearchResult[];
  processingTimeMs: number;
}
