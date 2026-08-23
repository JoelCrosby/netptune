import { Signal, Type } from '@angular/core';
import { Params } from '@angular/router';
import { ClientResponse } from '@app/core/models/client-response';
import { Page } from '@app/core/models/pagination';

export type DatatableSortDirection = 'asc' | 'desc';

export interface DatatableSort {
  sortBy: string;
  sortDirection: DatatableSortDirection;
}

export type DatatableAccessor<T> = keyof T | ((row: T) => unknown);
export type DatatableTrackBy<T> = (index: number, row: T) => unknown;
export type DatatableCellClass<T> =
  string | ((row: T, column: DatatableColumn<T>, rowIndex: number) => string);
export type DatatableRowClass<T> =
  string | ((row: T, rowIndex: number) => string);

export interface DatatableCellRenderer<T = unknown> {
  component: Type<unknown>;
  inputs?: (row: T) => Record<string, unknown>;
}

export interface DatatableColumn<T = unknown> {
  id: string;
  header: string;
  accessor?: DatatableAccessor<T>;
  sortable?: boolean;
  sortKey?: string;
  format?: (value: unknown, row: T, column: DatatableColumn<T>) => unknown;
  cell?: DatatableCellRenderer<T>;
  headerClass?: string;
  cellClass?: DatatableCellClass<T>;
  widthClass?: string;
  align?: 'start' | 'center' | 'end';
  ariaLabel?: string;
}

export interface DatatableColumnPreference {
  id: string;
  visible: boolean;
}

export interface DatatableMenuItem<T = unknown> {
  label: string;
  icon: Type<unknown>;
  onClick: (row: T) => void;
}

export interface DatatableLoadSort extends DatatableSort {
  field: string;
}

export interface DatatableLoadParams {
  sort: DatatableLoadSort | null;
  pageSize: number;
  page: number;
}

interface DatatableDataSourceBase<T = unknown> {
  key: string;
  columns: readonly DatatableColumn<T>[];
  trackBy: (index: number, row: T) => string | number;
  menu?: readonly DatatableMenuItem<T>[];
}

// Rows are fetched and paged by the table itself from a paginated GET endpoint.
export interface DatatableRemoteDataSource<
  T = unknown,
> extends DatatableDataSourceBase<T> {
  resource: {
    url: string;
    params: Signal<Params>;
  };
  rows?: (response: ClientResponse<Page<T>> | undefined) => readonly T[];
  reloadSignal?: Signal<unknown>;
}

// Rows are already resolved by the host. Used where the request the table would
// otherwise make is not a plain paginated GET, such as the POST-backed task view
// preview. Paging is the host's business, so the pager is hidden.
export interface DatatableLocalDataSource<
  T = unknown,
> extends DatatableDataSourceBase<T> {
  items: Signal<readonly T[]>;
  loading?: Signal<boolean>;
  totalCount?: Signal<number>;
}

export type DatatableDataSource<T = unknown> =
  DatatableRemoteDataSource<T> | DatatableLocalDataSource<T>;

export function isLocalDataSource<T>(
  source: DatatableDataSource<T>
): source is DatatableLocalDataSource<T> {
  return 'items' in source;
}

export interface DatatableCellContext<T = unknown> {
  $implicit: T;
  row: T;
  value: unknown;
  column: DatatableColumn<T>;
  rowIndex: number;
}
