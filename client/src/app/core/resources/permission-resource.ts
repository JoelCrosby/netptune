import {
  httpResource,
  HttpResourceOptions,
  HttpResourceRef,
  HttpResourceRequest,
} from '@angular/common/http';
import { assertInInjectionContext, Signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { RefreshScope } from '@core/models/refresh-scope';
import {
  reloadOnRefresh,
  reloadOnWorkspaceChange,
} from '@core/util/reload-on-refresh';
import { Permission } from '../auth/permissions';

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

  const canRead = hasPermission(permission);

  const { refreshOn, ...resourceOptions } = options ?? {};

  const resource = httpResource<T>(
    () => (canRead() ? request() : undefined),
    resourceOptions as HttpResourceOptions<T, unknown>
  );

  reloadOnWorkspaceChange(resource);

  if (refreshOn?.length) {
    reloadOnRefresh(resource, refreshOn);
  }

  return Object.assign(resource, { canRead });
}
