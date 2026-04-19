import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';

export const selectCanCreateComment = (store: Store) => {
  return store.selectSignal(
    selectHasPermission(netptunePermissions.comments.create)
  );
};

export const selectCanDeleteComment = (store: Store) => {
  return store.selectSignal(
    selectHasPermission(netptunePermissions.comments.deleteOwn)
  );
};

export const selectCanUpdateTask = (store: Store) => {
  return store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.update)
  );
};

export const selectCanDeleteTask = (store: Store) => {
  return store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.delete)
  );
};
