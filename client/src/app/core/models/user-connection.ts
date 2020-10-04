import { AppUser } from './appuser';

export interface UserConnection {
  connectionId: string;
  user: AppUser;
  userId: string;
}
