import { Injectable, Inject } from '@angular/core';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { AuthService } from '../auth/auth.service';
import { Post } from '../../models/post';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PostsService {

  constructor(
    private http: HttpClient,
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
      .get<Post[]>(this.baseUrl + 'api/GetProjectPosts' + '?projectId=' + projectId, httpOptions);
  }

  savePost(post: Post): Observable<Post> {
    const httpOptions = this.getHeaders();

    return this.http
      .post<Post>(this.baseUrl + 'api/PostPost', post, httpOptions);
  }
}
