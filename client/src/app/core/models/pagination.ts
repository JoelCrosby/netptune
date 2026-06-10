import { HttpHeaders, HttpParams } from '@angular/common/http';

export const DEFAULT_PAGE_SIZE = 50;
export const MAX_PAGE_SIZE = 100;
export const ADMIN_PAGE_SIZE = 200;

export interface CursorPage<T> {
  items: T[];
  nextCursor?: string;
  pageLimit: number;
}

export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CursorQuery {
  take?: number;
  cursor?: string | null;
}

export interface PageQuery {
  page?: number;
  pageSize?: number;
}

export function appendCursorParams(
  params = new HttpParams(),
  query?: CursorQuery
): HttpParams {
  const take = query?.take ?? DEFAULT_PAGE_SIZE;
  params = params.set('take', take);

  if (query?.cursor) {
    params = params.set('cursor', query.cursor);
  }

  return params;
}

export function appendPageParams(
  params = new HttpParams(),
  query?: PageQuery
): HttpParams {
  params = params.set('page', query?.page ?? 1);
  params = params.set('pageSize', query?.pageSize ?? MAX_PAGE_SIZE);

  return params;
}

export function cursorPageFromHeaders<T>(
  items: T[] | null,
  headers: HttpHeaders
): CursorPage<T> {
  return {
    items: items ?? [],
    nextCursor: headers.get('X-Next-Cursor') ?? undefined,
    pageLimit: Number(headers.get('X-Page-Limit') ?? DEFAULT_PAGE_SIZE),
  };
}
