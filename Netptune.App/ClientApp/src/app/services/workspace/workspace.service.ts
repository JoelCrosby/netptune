import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { environment } from '@env/environment';
import { Workspace } from '@app/models/workspace';
import { AuthService } from '../auth/auth.service';
import { Maybe } from '@app/core/types/nothing';
import { BaseService } from '../base.service';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {

  public onWorkspaceChanged = new Subject<Workspace>();

  constructor(
    private http: HttpClient,
    private baseService: BaseService,
    private authService: AuthService
  ) {
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
        if (workspace) {
          return workspace;
        }
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
      throw new Error('Cannot set current worksapce to undefined | null');
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

  getWorkspaces(): Observable<Workspace[]> {
    return this.http.get<Workspace[]>(environment.apiEndpoint + 'api/Workspaces', this.baseService.httpOptions);
  }

  addWorkspace(workspace: Workspace): Observable<Workspace> {
    return this.http.post<Workspace>(environment.apiEndpoint + 'api/Workspaces', workspace, this.baseService.httpOptions);
  }

  updateWorkspace(workspace: Workspace): Observable<Workspace> {
    const path = `${environment.apiEndpoint}api/Workspaces/${workspace.id}`;
    return this.http.put<Workspace>(path, workspace, this.baseService.httpOptions);
  }

  deleteWorkspace(workspace: Workspace): Observable<Workspace> {
    workspace.isDeleted = true;

    const path = `${environment.apiEndpoint}api/Workspaces/${workspace.id}`;
    return this.http.put<Workspace>(path, workspace, this.baseService.httpOptions);
  }

}
