import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppUser } from '../../models/appuser';
import { Workspace } from '../../models/workspace';
import { AuthService } from '../auth/auth.service';
import { WorkspaceService } from '../workspace/workspace.service';
import { UserSettings } from '../../models/user-settings';
import { MatSnackBar, MatDialog } from '@angular/material';
import { InviteDialogComponent } from '../../components/dialogs/invite-dialog/invite-dialog.component';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  public appUsers: AppUser[] = [];
  public settings = new UserSettings();
  public currentUser: AppUser;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private snackbar: MatSnackBar,
    private dialog: MatDialog,
    private workspaceService: WorkspaceService,
    @Inject('BASE_URL') private baseUrl: string) { }

  async refreshUsers(workspace: Workspace = this.workspaceService.currentWorkspace): Promise<void> {

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

    let user: AppUser = null;

    try {
      user = await this.getUserByEmail(email).toPromise();
    } catch (error) {
      this.snackbar.open(`User with specified email address does not exist.`,
        null,
        { duration: 2000 });
      return null;
    }

    try {
      const userResult = await this.inviteUser(user, this.workspaceService.currentWorkspace).toPromise();
      if (userResult) {
        this.refreshUsers();
        this.snackbar.open(`User ${userResult.email} has been invited to this workspace.`,
          null,
          { duration: 2000 });
      }
    } catch (error) {
      this.snackbar.open(`An error has occured while trying to invite the user to this workspace.`,
        null,
        { duration: 2000 });
    }
  }

  getHeaders() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + this.authService.token.token
      })
    };
  }

  getUsers(workspace: Workspace): Observable<AppUser[]> {
    const httpOptions = this.getHeaders();
    return this.http.get<AppUser[]>(environment.apiEndpoint + 'api/AppUsers' + `?workspaceId=${workspace.id}`, httpOptions);
  }

  getUser(userId: string = this.authService.token.userId): Observable<AppUser> {
    const httpOptions = this.getHeaders();
    return this.http.get<AppUser>(environment.apiEndpoint + 'api/AppUsers/' + userId, httpOptions);
  }

  getUserByEmail(email: string): Observable<AppUser> {
    const httpOptions = this.getHeaders();
    return this.http.get<AppUser>(environment.apiEndpoint + `api/AppUsers/GetUserByEmail?email=${email}`, httpOptions);
  }

  inviteUser(user: AppUser, workspace: Workspace): Observable<AppUser> {
    const httpOptions = this.getHeaders();
    return this.http.post<AppUser>(environment.apiEndpoint + `api/AppUsers/Invite?userId=${user.id}&workspaceId=${workspace.id}`,
      httpOptions);
  }

  updateUser(user: AppUser = this.currentUser): Observable<AppUser> {
    const httpOptions = this.getHeaders();
    return this.http.post<AppUser>(environment.apiEndpoint + `api/AppUsers/UpdateUser`, user, httpOptions);
  }

}
