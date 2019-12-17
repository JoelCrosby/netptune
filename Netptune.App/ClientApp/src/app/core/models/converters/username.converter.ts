import { AppUser } from '../appuser';

export class UsernameConverter {
  static toDisplay(user: AppUser): string {
    if (!user) {
      return '';
    }
    if (user.firstname && user.lastname) {
      return `${user.firstname} ${user.lastname}`;
    }
    return user.email;
  }
}
