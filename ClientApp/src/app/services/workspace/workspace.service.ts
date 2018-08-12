import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '../../../../node_modules/@angular/common/http';
import { AuthService } from '../auth/auth.service';
import { Observable, throwError } from '../../../../node_modules/rxjs';
import { Workspace } from '../../models/workspace';
import { catchError } from '../../../../node_modules/rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {

  constructor(private http: HttpClient, private authService: AuthService, @Inject('BASE_URL') private baseUrl: string) { }

  public currentWorkspace: Workspace;

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

    const url = `${this.baseUrl}api/Workspaces/${workspace.workspaceId}`;
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
