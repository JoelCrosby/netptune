import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Workspace } from '../../models/workspace';
import { AuthService } from '../auth/auth.service';
import { Maybe } from '../../modules/nothing';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {

  public onWorkspaceChanged = new Subject<Workspace>();

  constructor(
    private http: HttpClient,
    private authService: AuthService) {
    this.authService.onLogout.subscribe(() => {
      this.clearCurrentWorkspace();
    });
  }

  public workspaces: Workspace[] = [];

  public get currentWorkspace(): Maybe<Workspace> {
    const res = localStorage.getItem('currentWorkspace');
    if (res) {
      try {
        const workspace = JSON.parse(res);
        if (workspace) return workspace;
      } catch {
        return;
      }
    }
  }
  public set currentWorkspace(value: Maybe<Workspace>) {
    if (value) {
      localStorage.setItem('currentWorkspace', JSON.stringify(value));
      this.onWorkspaceChanged.next(value);
    } else {
      throw new Error('Cannot set current worksapce to undefined | null')
    }
  }

  clearCurrentWorkspace() {
    localStorage.removeItem('currentWorkspace');
  }

  refreshWorkspaces() {
    this.getWorkspaces()
      .subscribe(workspaces => {
        this.workspaces.splice(0, this.workspaces.length);
        this.workspaces.push(...workspaces);
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

  getWorkspaces(): Observable<Workspace[]> {
    const httpOptions = this.getHeaders();

    return this.http.get<Workspace[]>(environment.apiEndpoint + 'api/Workspaces', httpOptions);
  }

  addWorkspace(workspace: Workspace): Observable<Workspace> {
    const httpOptions = this.getHeaders();

    return this.http.post<Workspace>(environment.apiEndpoint + 'api/Workspaces', workspace, httpOptions);
  }

  updateWorkspace(workspace: Workspace): Observable<Workspace> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/Workspaces/${workspace.id}`;
    return this.http.put<Workspace>(url, workspace, httpOptions);
  }

  deleteWorkspace(workspace: Workspace): Observable<Workspace> {

    workspace.isDeleted = true;

    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/Workspaces/${workspace.id}`;
    return this.http.put<Workspace>(url, workspace, httpOptions);
  }

}
