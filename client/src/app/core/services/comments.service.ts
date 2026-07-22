import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { CommentViewModel } from '@core/models/comment';
import {
  AddCommentRequest,
  UpdateCommentRequest,
} from '@core/models/requests/add-comment-request';

@Injectable({
  providedIn: 'root',
})
export class CommentsService {
  private http = inject(HttpClient);

  postToTask(request: AddCommentRequest) {
    return this.http.post<ClientResponse<CommentViewModel>>(
      'api/comments/task',
      request
    );
  }

  update(commentId: number, request: UpdateCommentRequest) {
    return this.http.put<ClientResponse<CommentViewModel>>(
      `api/comments/${commentId}`,
      request
    );
  }

  delete(commentId: number) {
    return this.http.delete<ClientResponse>(`api/comments/${commentId}`);
  }
}
