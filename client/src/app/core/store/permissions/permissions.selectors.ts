import { PERMISSONS } from '@core/auth/permissions';
import { hasPermission } from '@core/auth/has-permission';

export const selectCanCreateComment = () => {
  return hasPermission(PERMISSONS.comments.create);
};

export const selectCanDeleteComment = () => {
  return hasPermission(PERMISSONS.comments.deleteOwn);
};

export const selectCanUpdateTask = () => {
  return hasPermission(PERMISSONS.tasks.update);
};

export const selectCanDeleteTask = () => {
  return hasPermission(PERMISSONS.tasks.delete);
};
