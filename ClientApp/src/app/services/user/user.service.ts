import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppUser } from '../../models/appuser';
import { Workspace } from '../../models/workspace';
import { AuthService } from '../auth/auth.service';
import { WorkspaceService } from '../workspace/workspace.service';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  public appUsers: AppUser[];

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private workspaceService: WorkspaceService,
    @Inject('BASE_URL') private baseUrl: string) { }

  refreshUsers(workspace: Workspace = this.workspaceService.currentWorkspace): void {

    this.getUsers(workspace ? workspace : this.workspaceService.currentWorkspace)
      .subscribe(appUsers => { this.appUsers = appUsers; console.log(appUsers); });
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
    return this.http.get<AppUser[]>(this.baseUrl + 'api/AppUsers' + `?workspaceId=${workspace.workspaceId}`, httpOptions);
  }

  getUser(userId: string): Observable<AppUser> {
    const httpOptions = this.getHeaders();
    return this.http.get<AppUser>(this.baseUrl + 'api/AppUsers/' + userId, httpOptions);
  }

}
