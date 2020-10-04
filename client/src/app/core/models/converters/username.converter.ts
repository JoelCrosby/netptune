import { AppUser } from '../appuser';

export function toDisplay(user: AppUser): string {
  if (user.firstname && user.lastname) {
    return `${user.firstname} ${user.lastname}`;
  }
  return user.email;
}
