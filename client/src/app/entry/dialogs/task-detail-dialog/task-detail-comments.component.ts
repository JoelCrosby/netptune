import { httpResource } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { DEFAULT_PAGE_SIZE } from '@app/core/models/pagination';
import { ConfirmationService } from '@app/core/services/confirmation.service';
import { CommentsService } from '@app/core/services/comments.service';
import {
  selectCanCreateComment,
  selectCanDeleteComment,
} from '@app/core/store/permissions/permissions.selectors';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { selectCurrentUser } from '@app/core/store/auth/auth.selectors';
import { CommentViewModel } from '@core/models/comment';
import { AddCommentRequest } from '@core/models/requests/add-comment-request';
import { selectAllUsers } from '@core/store/users/users.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { Store } from '@ngrx/store';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  CommentsListComponent,
  CommentSubmitEvent,
} from '@static/components/comments-list/comments-list.component';
import { EMPTY } from 'rxjs';
import { catchError, filter, switchMap, tap } from 'rxjs/operators';
import { ConfirmDialogOptions } from '../confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-task-detail-comments',
  template: `
    <h4 class="font-sm mt-4 mb-2 font-semibold">Comments</h4>
    <app-comments-list
      [user]="user()"
      [comments]="comments.value()"
      [workspaceUsers]="workspaceUsers()"
      (commentSubmit)="onCommentSubmit($event)"
      (deleteComment)="onDeleteCommentClicked($event)"
      [canDelete]="canDeleteComment()"
      [canCreate]="canCreateComment()">
    </app-comments-list>
  `,
  imports: [CommentsListComponent],
})
export class TaskDetailCommentsComponent {
  store = inject(Store);
  commentsService = inject(CommentsService);
  confirmation = inject(ConfirmationService);
  snackbar = inject(SnackbarService);

  user = this.store.selectSignal(selectCurrentUser);
  workspaceUsers = this.store.selectSignal(selectAllUsers);

  task = this.store.selectSignal(selectDetailTask);

  comments = httpResource<CommentViewModel[]>(
    () => {
      const systemId = this.task()?.systemId;

      if (!systemId) {
        return undefined;
      }

      return {
        url: `api/comments/task/${systemId}`,
        params: {
          page: 1,
          pageSize: DEFAULT_PAGE_SIZE,
        },
      };
    },
    { defaultValue: [] }
  );

  canCreateComment = selectCanCreateComment(this.store);
  canDeleteComment = selectCanDeleteComment(this.store);

  onCommentSubmit(event: CommentSubmitEvent) {
    if (!event.text) return;

    const task = this.task();

    if (!task) return;

    const request: AddCommentRequest = {
      comment: event.text,
      systemId: task.systemId,
      mentions: event.mentions,
    };

    this.commentsService
      .postToTask(request)
      .pipe(
        unwrapClientReposne(),
        tap(() => this.comments.reload()),
        catchError(() => EMPTY)
      )
      .subscribe();
  }

  onDeleteCommentClicked(comment: CommentViewModel) {
    this.confirmation
      .open(DELETE_COMMENT_CONFIRMATION)
      .pipe(
        filter(Boolean),
        switchMap(() => this.commentsService.delete(comment.id)),
        unwrapClientReposne(),
        tap(() => {
          this.snackbar.open('Comment deleted');
          this.comments.reload();
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }
}

const DELETE_COMMENT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this comment?',
  title: 'Delete Comment',
  color: 'warn',
};
