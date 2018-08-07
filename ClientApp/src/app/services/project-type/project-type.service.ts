import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';
import { ProjectType } from '../../models/project-type';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root'
})
export class ProjectTypeService {

  getHeaders() {
    return { headers: new HttpHeaders({
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + this.authService.token.token
      })
    };
  }

  constructor(private http: HttpClient, private authService: AuthService, @Inject('BASE_URL') private baseUrl: string) { }

  getProjectTypes(): Observable<ProjectType[]> {
    const httpOptions = this.getHeaders();

    return this.http.get<ProjectType[]>(this.baseUrl + 'api/ProjectTypes', httpOptions)
    .pipe(
      catchError(this.handleError)
    );
  }

  getProjectType(id: number): Observable<ProjectType> {
    const url = `${this.baseUrl}api/ProjectTypes/${id}`;
    return this.http.get<ProjectType>(url)
    .pipe(
      catchError(this.handleError)
    );
  }

  addProjectType(project: ProjectType): Observable<ProjectType> {
    const httpOptions = this.getHeaders();

    return this.http.post<ProjectType>(this.baseUrl + 'api/ProjectTypes', project, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  updateProjectType(project: ProjectType): Observable<ProjectType> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/ProjectTypes/${project.projectTypeId}`;
    return this.http.put<ProjectType>(url, project, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  deleteProjectType(project: ProjectType): Observable<ProjectType> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/ProjectTypes/${project.projectTypeId}`;
    return this.http.delete<ProjectType>(url, httpOptions)
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

