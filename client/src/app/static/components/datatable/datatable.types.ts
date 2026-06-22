import { HttpResourceRef } from '@angular/common/http';
import { Injector, Signal, Type } from '@angular/core';
import { ClientResponse } from '@app/core/models/client-response';
import { Page } from '@app/core/models/pagination';

export type DatatableSortDirection = 'asc' | 'desc';

export interface DatatableSort {
  columnId: string;
  direction: DatatableSortDirection;
}

export type DatatableAccessor<T> = keyof T | ((row: T) => unknown);
export type DatatableTrackBy<T> = (index: number, row: T) => unknown;
export type DatatableCellClass<T> =
  | string
  | ((row: T, column: DatatableColumn<T>, rowIndex: number) => string);
export type DatatableRowClass<T> =
  | string
  | ((row: T, rowIndex: number) => string);

export interface DatatableColumn<T = unknown> {
  id: string;
  header: string;
  accessor?: DatatableAccessor<T>;
  sortable?: boolean;
  sortKey?: string;
  format?: (value: unknown, row: T, column: DatatableColumn<T>) => unknown;
  headerClass?: string;
  cellClass?: DatatableCellClass<T>;
  widthClass?: string;
  align?: 'start' | 'center' | 'end';
  ariaLabel?: string;
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
}

export interface DatatableDataSource<T = unknown> {
  columns: readonly DatatableColumn<T>[];
  resource: (
    params: Signal<DatatableLoadParams>,
    injector: Injector
  ) => HttpResourceRef<ClientResponse<Page<T>>>;
  rows?: (response: ClientResponse<Page<T>> | undefined) => readonly T[];
  trackBy: (index: number, row: T) => string | number;
  menu?: readonly DatatableMenuItem<T>[];
}

export interface DatatableCellContext<T = unknown> {
  $implicit: T;
  row: T;
  value: unknown;
  column: DatatableColumn<T>;
  rowIndex: number;
}
