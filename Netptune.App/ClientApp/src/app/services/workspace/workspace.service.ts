import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, Subject } from 'rxjs';
import { Workspace } from '../../models/workspace';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {

  public onWorkspaceChanged = new Subject<Workspace>();

  constructor(
    private http: HttpClient,
    private authService: AuthService) {
    this.authService.onLogout.subscribe(() => {
      this.currentWorkspace = null;
    });
  }

  public workspaces: Workspace[] = [];

  public get currentWorkspace(): Workspace {
    return JSON.parse(localStorage.getItem('currentWorkspace'));
  }
  public set currentWorkspace(value: Workspace) {
    localStorage.setItem('currentWorkspace', JSON.stringify(value));
    this.onWorkspaceChanged.next(this.currentWorkspace);
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

    return this.http.get<Workspace[]>(environment.apiEndpoint + 'api/Workspaces', httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  addWorkspace(workspace: Workspace): Observable<Workspace> {
    const httpOptions = this.getHeaders();

    return this.http.post<Workspace>(environment.apiEndpoint + 'api/Workspaces', workspace, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  updateWorkspace(workspace: Workspace): Observable<Workspace> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/Workspaces/${workspace.id}`;
    return this.http.put<Workspace>(url, workspace, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  deleteWorkspace(workspace: Workspace): Observable<Workspace> {

    workspace.isDeleted = true;

    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/Workspaces/${workspace.id}`;
    return this.http.put<Workspace>(url, workspace, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  private handleError(error: HttpErrorResponse) {
    if (error.error instanceof ErrorEvent) {
      // A client-side or network error occurred. Handle it accordingly.
      console.error('An error occurred:', error.error.message);
    } else {
      // The backend returned an unsuccessful response code.
      // The response body may contain clues as to what went wrong,
      console.error(
        `Backend returned code ${error.status}, ` +
        `body was: ${error.error}`);
    }
    // return an observable with a user-facing error message
    return throwError(
      'Something bad happened; please try again later.');
  }

}
