import { AppUser } from './appuser';

export class Basemodel {

    public isDeleted: Boolean;

    public createdAt: Date;
    public updatedAt: Date;

    public createdByUser: AppUser;
    public modifiedByUser: AppUser;
    public deletedByUser: AppUser;

    public owner: AppUser;

}
