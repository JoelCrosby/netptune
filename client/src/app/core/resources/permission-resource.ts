import {
  httpResource,
  HttpResourceOptions,
  HttpResourceRef,
  HttpResourceRequest,
} from '@angular/common/http';
import { assertInInjectionContext, inject, Signal } from '@angular/core';
import { Store } from '@ngrx/store';
import { Permission } from '../auth/permissions';
import { selectHasPermission } from '../store/auth/auth.selectors';

export type PermissionResourceRef<T> = HttpResourceRef<T> & {
  readonly canRead: Signal<boolean>;
};

export function permissionResource<T>(
  permission: Permission,
  request: () => HttpResourceRequest | undefined,
  options: HttpResourceOptions<T, unknown> & { defaultValue: NoInfer<T> }
): PermissionResourceRef<T>;

export function permissionResource<T>(
  permission: Permission,
  request: () => HttpResourceRequest | undefined,
  options?: HttpResourceOptions<T, unknown>
): PermissionResourceRef<T | undefined>;

export function permissionResource<T>(
  permission: Permission,
  request: () => HttpResourceRequest | undefined,
  options?: HttpResourceOptions<T, unknown>
): PermissionResourceRef<T | undefined> {
  assertInInjectionContext(permissionResource);

  const store = inject(Store);
  const canRead = store.selectSignal(selectHasPermission(permission));

  const resource = httpResource<T>(
    () => (canRead() ? request() : undefined),
    options
  );

  return Object.assign(resource, { canRead });
}
