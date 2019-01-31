import { Injectable, Inject } from '@angular/core';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { Post } from '../../models/post';
import { Observable } from 'rxjs';
import { MatSnackBar } from '@angular/material';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { Maybe } from '../../modules/nothing';

@Injectable({
  providedIn: 'root'
})
export class PostsService {

  constructor(
    private http: HttpClient,
    private snackBar: MatSnackBar,
    private authService: AuthService
  ) { }

  getHeaders() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: 'Bearer ' + this.authService.token.token
      })
    };
  }

  getProjectPosts(projectId: string): Observable<Post[]> {
    const httpOptions = this.getHeaders();

    return this.http
      .get<Post[]>(environment.apiEndpoint + 'api/Posts/GetProjectPosts' + '?projectId=' + projectId, httpOptions);
  }

  async savePost(post: Post): Promise<Maybe<Post>> {
    const httpOptions = this.getHeaders();

    try {
      const result =
        await this.http.post<Post>(environment.apiEndpoint + 'api/Posts/', post, httpOptions).toPromise();

      this.snackBar.open('Task Deleted', undefined, {
        duration: 2000
      });
      return result;
    } catch {
      this.snackBar.open('An error occured while trying to delete task.', undefined, {
        duration: 2000
      });
    }
  }
}
