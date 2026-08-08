import {
  httpResource,
  HttpResourceOptions,
  HttpResourceRef,
  HttpResourceRequest,
} from '@angular/common/http';
import { assertInInjectionContext, inject, Signal } from '@angular/core';
import { RefreshScope } from '@core/models/refresh-scope';
import { reloadOnRefresh } from '@core/util/reload-on-refresh';
import { Store } from '@ngrx/store';
import { Permission } from '../auth/permissions';
import { selectHasPermission } from '../store/auth/auth.selectors';

export type PermissionResourceRef<T> = HttpResourceRef<T> & {
  readonly canRead: Signal<boolean>;
};

export type PermissionResourceOptions<T> = HttpResourceOptions<T, unknown> & {
  /** Scopes that make this resource stale — it reloads when one of them changes. */
  refreshOn?: readonly RefreshScope[];
};

export function permissionResource<T>(
  permission: Permission,
  request: () => HttpResourceRequest | undefined,
  options: PermissionResourceOptions<T> & { defaultValue: NoInfer<T> }
): PermissionResourceRef<T>;

export function permissionResource<T>(
  permission: Permission,
  request: () => HttpResourceRequest | undefined,
  options?: PermissionResourceOptions<T>
): PermissionResourceRef<T | undefined>;

export function permissionResource<T>(
  permission: Permission,
  request: () => HttpResourceRequest | undefined,
  options?: PermissionResourceOptions<T>
): PermissionResourceRef<T | undefined> {
  assertInInjectionContext(permissionResource);

  const store = inject(Store);
  const canRead = store.selectSignal(selectHasPermission(permission));

  const { refreshOn, ...resourceOptions } = options ?? {};

  const resource = httpResource<T>(
    () => (canRead() ? request() : undefined),
    resourceOptions as HttpResourceOptions<T, unknown>
  );

  if (refreshOn?.length) {
    reloadOnRefresh(resource, refreshOn);
  }

  return Object.assign(resource, { canRead });
}
