import {
  httpResource,
  HttpResourceOptions,
  HttpResourceRef,
  HttpResourceRequest,
} from '@angular/common/http';
import { assertInInjectionContext, signal, Signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { RefreshScope } from '@core/models/refresh-scope';
import {
  reloadOnRefresh,
  reloadOnWorkspaceChange,
} from '@core/util/reload-on-refresh';
import { ClientResponse } from '../models/client-response';
import { Permission } from '../auth/permissions';

/** Stands in for the permission check when a resource is not gated. */
const alwaysReadable = signal(true).asReadonly();

export type PermissionResourceRef<T> = HttpResourceRef<T> & {
  readonly canRead: Signal<boolean>;
};

/**
 * Every app endpoint answers with a `ClientResponse` envelope, so `parse` receives
 * one by default and a call site can write `response.payload` without casting.
 * Endpoints whose raw body is shaped differently — a page inside the envelope, say
 * — name that shape as the second type argument.
 */
export type PermissionResourceConfig<
  T,
  TRaw = ClientResponse<T>,
> = HttpResourceOptions<T, TRaw> & {
  /** The request to make. Returning undefined idles the resource. */
  request: () => HttpResourceRequest | undefined;
  /** Gates the request. Omit to fetch for everyone. */
  permission?: Permission;
  /** Scopes that make this resource stale — it reloads when one of them changes. */
  refreshOn?: readonly RefreshScope[];
};

export function permissionResource<T, TRaw = ClientResponse<T>>(
  config: PermissionResourceConfig<T, TRaw> & { defaultValue: NoInfer<T> }
): PermissionResourceRef<T>;

export function permissionResource<T, TRaw = ClientResponse<T>>(
  config: PermissionResourceConfig<T, TRaw>
): PermissionResourceRef<T | undefined>;

export function permissionResource<T, TRaw = ClientResponse<T>>(
  config: PermissionResourceConfig<T, TRaw>
): PermissionResourceRef<T | undefined> {
  assertInInjectionContext(permissionResource);

  const { request, permission, refreshOn, ...resourceOptions } = config;

  const canRead = permission ? hasPermission(permission) : alwaysReadable;

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
