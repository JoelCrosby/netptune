import { AppUser } from './appuser';

export interface Basemodel {
  id?: number;

  isDeleted?: boolean;

  createdAt?: Date;
  updatedAt?: Date;

  createdByUser?: AppUser;
  createdByUserId?: string;

  modifiedByUser?: AppUser;
  modifiedByUserId?: string;

  deletedByUser?: AppUser;
  deletedByUserId?: string;

  owner?: AppUser;
  ownerId?: string;
}
