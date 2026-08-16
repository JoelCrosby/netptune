import { HttpResourceRequest } from '@angular/common/http';
import {
  assertInInjectionContext,
  linkedSignal,
  ResourceRef,
  Signal,
  WritableSignal,
} from '@angular/core';
import { Permission } from '../auth/permissions';
import {
  permissionResource,
  PermissionResourceOptions,
} from './permission.resource';

export interface StableResourceRef<T> {
  /** Writable, so a view can show an edit before the server has agreed to it. */
  readonly value: WritableSignal<T>;
  /** The value as it last arrived, absent while a request is in flight. */
  readonly loadedValue: Signal<T>;
  readonly isLoading: Signal<boolean>;
  readonly error: Signal<unknown>;
  readonly canRead: Signal<boolean>;
  reload(): void;
}

/**
 * A `permissionResource` that holds its previous value for the duration of the next
 * request, rather than blanking and tearing down whatever the view rendered from it.
 */
export function stableResource<T>(
  permission: Permission,
  request: () => HttpResourceRequest | undefined,
  options?: PermissionResourceOptions<T>
): StableResourceRef<T | undefined> {
  assertInInjectionContext(stableResource);

  const resource = permissionResource<T>(permission, request, options);

  return {
    value: retainWhileLoading(resource),
    loadedValue: resource.value,
    isLoading: resource.isLoading,
    error: resource.error,
    canRead: resource.canRead,
    reload: () => resource.reload(),
  };
}

/** The retention on its own, for a resource built some other way. */
export function retainWhileLoading<T>(
  resource: ResourceRef<T>
): WritableSignal<T> {
  return linkedSignal<T, T>({
    source: resource.value,
    computation: (next, previous) => {
      if (!previous) return next;

      return resource.isLoading() ? previous.value : next;
    },
  });
}
