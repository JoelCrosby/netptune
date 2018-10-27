import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppUser } from '../../models/appuser';
import { Workspace } from '../../models/workspace';
import { AuthService } from '../auth/auth.service';
import { WorkspaceService } from '../workspace/workspace.service';
import { UserSettings } from '../../models/user-settings';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  public appUsers: AppUser[] = [];
  public settings = new UserSettings();

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private workspaceService: WorkspaceService,
    @Inject('BASE_URL') private baseUrl: string) { }

  refreshUsers(workspace: Workspace = this.workspaceService.currentWorkspace): void {

    this.getUsers(workspace ? workspace : this.workspaceService.currentWorkspace)
      .subscribe(appUsers => {
        this.appUsers.splice(0, this.appUsers.length);
        this.appUsers.push(...appUsers);
      });
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
    return this.http.get<AppUser[]>(this.baseUrl + 'api/AppUsers' + `?workspaceId=${workspace.id}`, httpOptions);
  }

  getUser(userId: string = this.authService.token.userId): Observable<AppUser> {
    const httpOptions = this.getHeaders();
    return this.http.get<AppUser>(this.baseUrl + 'api/AppUsers/' + userId, httpOptions);
  }

  getUserByEmail(email: string): Observable<AppUser> {
    const httpOptions = this.getHeaders();
    return this.http.get<AppUser>(this.baseUrl + `api/AppUsers/GetUserByEmail?email=${email}`, httpOptions);
  }

  inviteUser(user: AppUser, workspace: Workspace): Observable<AppUser> {
    const httpOptions = this.getHeaders();
    return this.http.post<AppUser>(this.baseUrl + `api/AppUsers/Invite?userId=${user.id}&workspaceId=${workspace.id}`, httpOptions);
  }

}
