import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppUser } from '@app/models/appuser';
import { Workspace } from '@app/models/workspace';
import { WorkspaceService } from '../workspace/workspace.service';
import { UserSettings } from '@app/models/user-settings';
import { MatSnackBar, MatDialog } from '@angular/material';
import { InviteDialogComponent } from '@app/dialogs/invite-dialog/invite-dialog.component';
import { environment } from '@env/environment';
import { ApiResult } from '@app/models/api-result';
import { AuthService } from '../auth/auth.service';
import { Maybe } from '@app/core/types/nothing';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  appUsers: AppUser[] = [];
  settings = new UserSettings();
  currentUser: AppUser;

  get getHeaders() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + this.authService.token.token
      })
    };
  }

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private snackbar: MatSnackBar,
    private dialog: MatDialog,
    private workspaceService: WorkspaceService) { }

  async refreshUsers(workspace: Maybe<Workspace> = this.workspaceService.currentWorkspace): Promise<void> {

    if (!workspace) throw new Error('Unable to determine workspace to refresh users for');

    const appUsers = await this.getUsers(workspace).toPromise();

    this.appUsers.splice(0, this.appUsers.length);
    this.appUsers.push(...appUsers);
  }

  async showInviteUserDialog(): Promise<void> {
    const dialogRef = this.dialog.open(InviteDialogComponent, {
      width: '600px'
    });

    const email: string = await dialogRef.afterClosed().toPromise();

    if (!email) {
      return;
    }

    if (!this.workspaceService.currentWorkspace) {
      throw new Error('cannot show user invite dialog while currentworkspace is undefined.');
    }

    let user: AppUser;

    try {
      user = await this.getUserByEmail(email).toPromise();
    } catch (error) {
      this.snackbar.open(`User with specified email address does not exist.`,
        undefined,
        { duration: 2000 });
      return;
    }

    try {
      const userResult = await this.inviteUser(user, this.workspaceService.currentWorkspace).toPromise();
      if (userResult) {
        this.refreshUsers();
        this.snackbar.open(`User ${userResult.email} has been invited to this workspace.`,
          undefined,
          { duration: 2000 });
      }
    } catch (error) {
      this.snackbar.open(`An error has occured while trying to invite the user to this workspace.`,
        undefined,
        { duration: 2000 });
    }
  }

  getUsers(workspace: Workspace): Observable<AppUser[]> {
    return this.http.get<AppUser[]>(environment.apiEndpoint + 'api/Users' + `?workspaceId=${workspace.id}`, this.getHeaders);
  }

  getUser(userId: string = this.authService.token.userId): Observable<AppUser> {
    return this.http.get<AppUser>(environment.apiEndpoint + 'api/Users/' + userId, this.getHeaders);
  }

  getUserByEmail(email: string): Observable<AppUser> {
    return this.http.get<AppUser>(environment.apiEndpoint + `api/Users/GetUserByEmail?email=${email}`, this.getHeaders);
  }

  inviteUser(user: AppUser, workspace: Workspace): Observable<AppUser> {
    return this.http.post<AppUser>(environment.apiEndpoint + `api/Users/Invite?userId=${user.id}&workspaceId=${workspace.id}`,
      this.getHeaders);
  }

  async updateUser(user: AppUser = this.currentUser): Promise<ApiResult> {

    try {
      await this.http.post<AppUser>(
        environment.apiEndpoint + `api/Users/UpdateUser`,
        user,
        this.getHeaders).toPromise();
      return ApiResult.Success();

    } catch (error) {
      return ApiResult.FromError(error);
    }
  }

}
