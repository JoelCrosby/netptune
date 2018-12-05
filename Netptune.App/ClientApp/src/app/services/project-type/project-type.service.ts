import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';
import { ProjectType } from '../../models/project-type';
import { AuthService } from '../auth/auth.service';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProjectTypeService {

  public projectTypes: ProjectType[] = [];

  getHeaders() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + this.authService.token.token
      })
    };
  }

  constructor(private http: HttpClient, private authService: AuthService, @Inject('BASE_URL') private baseUrl: string) { }

  async refreshProjectTypes(): Promise<void> {
    const response = await this.getProjectTypes().toPromise();

    this.projectTypes.splice(0, this.projectTypes.length);
    this.projectTypes.push.apply(this.projectTypes, response);
  }

  getProjectTypes(): Observable<ProjectType[]> {
    const httpOptions = this.getHeaders();

    return this.http.get<ProjectType[]>(environment.apiEndpoint + 'api/ProjectTypes', httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  getProjectType(id: number): Observable<ProjectType> {
    const url = `${environment.apiEndpoint}api/ProjectTypes/${id}`;
    return this.http.get<ProjectType>(url)
      .pipe(
        catchError(this.handleError)
      );
  }

  addProjectType(projectType: ProjectType): Observable<ProjectType> {
    const httpOptions = this.getHeaders();

    return this.http.post<ProjectType>(environment.apiEndpoint + 'api/ProjectTypes', projectType, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  updateProjectType(projectType: ProjectType): Observable<ProjectType> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/ProjectTypes/${projectType.id}`;
    return this.http.put<ProjectType>(url, projectType, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  deleteProjectType(projectType: ProjectType): Observable<ProjectType> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/ProjectTypes/${projectType.id}`;
    return this.http.delete<ProjectType>(url, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  getFaIconById(projectTypeId: number): string {

    if (!projectTypeId || !this.projectTypes) { return; }

    const projectType = this.projectTypes.filter(item => item.id === projectTypeId)[0];

    return this.getFaIcon(projectType);
  }

  getFaIcon(projectType: ProjectType): string {

    if (!projectType) { return null; }

    switch (projectType.typeCode) {
      case 'node':
        return 'mdi mdi-nodejs nodejs';
      case 'angular':
        return 'mdi mdi-angular angular';
      case 'winforms':
        return 'mdi mdi-windows windows';
      case 'aspcore':
        return 'mdi mdi-visual-studio visual-studio';
    }
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

