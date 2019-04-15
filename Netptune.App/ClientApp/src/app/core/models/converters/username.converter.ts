import { AppUser } from '../appuser';

export class UsernameConverter {
  static toDisplay(user: AppUser): string {
    if (!user) {
      return '';
    }
    if (user.firstName && user.lastName) {
      return `${user.firstName} ${user.lastName}`;
    }
    return user.email;
  }
}
