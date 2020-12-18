import { AppUser } from '../appuser';

export const toDisplay = (user: AppUser): string => {
  if (user.firstname && user.lastname) {
    return `${user.firstname} ${user.lastname}`;
  }
  return user.email;
};
