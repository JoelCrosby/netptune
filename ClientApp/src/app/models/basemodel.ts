import { AppUser } from './appuser';

export class Basemodel {

    public id: number;

    public isDeleted: Boolean;

    public createdAt: Date;
    public updatedAt: Date;

    public createdByUser: AppUser;
    public createdByUserId: string;

    public modifiedByUser: AppUser;
    public modifiedByUserId: string;

    public deletedByUser: AppUser;
    public deletedByUserId: string;

    public owner: AppUser;
    public ownerId: string;

}
