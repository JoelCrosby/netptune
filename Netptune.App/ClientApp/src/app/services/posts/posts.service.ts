import { Injectable, Inject } from '@angular/core';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { AuthService } from '../auth/auth.service';
import { Post } from '../../models/post';
import { Observable } from 'rxjs';
import { MatSnackBar } from '@angular/material';

@Injectable({
  providedIn: 'root'
})
export class PostsService {

  constructor(
    private http: HttpClient,
    private snackBar: MatSnackBar,
    private authService: AuthService,
    @Inject('BASE_URL') private baseUrl: string
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
      .get<Post[]>(this.baseUrl + 'api/Posts/GetProjectPosts' + '?projectId=' + projectId, httpOptions);
  }

  async savePost(post: Post): Promise<Post> {
    const httpOptions = this.getHeaders();

    try {
      const result =
        await this.http.post<Post>(this.baseUrl + 'api/Posts/', post, httpOptions).toPromise();

      this.snackBar.open('Task Deleted', null, {
        duration: 2000
      });
      return result;
    } catch {
      this.snackBar.open('An error occured while trying to delete task.', null, {
        duration: 2000
      });
    }
  }
}
