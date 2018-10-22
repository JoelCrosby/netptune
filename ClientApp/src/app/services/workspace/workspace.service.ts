import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../auth/auth.service';
import { Observable, throwError, Subject } from 'rxjs';
import { Workspace } from '../../models/workspace';
import { catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {

  public onWorkspaceChanged = new Subject<Workspace>();

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    @Inject('BASE_URL') private baseUrl: string) {
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

    return this.http.get<Workspace[]>(this.baseUrl + 'api/Workspaces', httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  addWorkspace(workspace: Workspace): Observable<Workspace> {
    const httpOptions = this.getHeaders();

    return this.http.post<Workspace>(this.baseUrl + 'api/Workspaces', workspace, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  updateWorkspace(workspace: Workspace): Observable<Workspace> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/Workspaces/${workspace.id}`;
    return this.http.put<Workspace>(url, workspace, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  deleteWorkspace(workspace: Workspace): Observable<Workspace> {

    workspace.isDeleted = true;

    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/Workspaces/${workspace.id}`;
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
