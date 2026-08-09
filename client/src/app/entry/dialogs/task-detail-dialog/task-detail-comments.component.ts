import { httpResource } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { DEFAULT_PAGE_SIZE } from '@app/core/models/pagination';
import { ConfirmationService } from '@app/core/services/confirmation.service';
import { CommentsService } from '@app/core/services/comments.service';
import {
  selectCanCreateComment,
  selectCanDeleteComment,
} from '@app/core/store/permissions/permissions.selectors';
import { CommentViewModel } from '@core/models/comment';
import {
  AddCommentRequest,
  UpdateCommentRequest,
} from '@core/models/requests/add-comment-request';
import { workspaceUsersResource } from '@core/resources/user.resource';
import { reloadOnRefresh } from '@core/util/reload-on-refresh';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  CommentsListComponent,
  CommentSubmitEvent,
  CommentUpdateEvent,
} from '@static/components/comments-list/comments-list.component';
import { EMPTY } from 'rxjs';
import { catchError, filter, switchMap, tap } from 'rxjs/operators';
import { ConfirmDialogOptions } from '../confirm-dialog/confirm-dialog.component';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-comments',
  template: `
    <h4 class="font-sm mt-4 mb-2 font-semibold">
      <span i18n="Section heading for a task's comments">Comments</span>
    </h4>
    <app-comments-list
      [user]="user()"
      [comments]="comments.value()"
      [workspaceUsers]="workspaceUsers()"
      (commentSubmit)="onCommentSubmit($event)"
      (updateComment)="onUpdateComment($event)"
      (deleteComment)="onDeleteCommentClicked($event)"
      [canDelete]="canDeleteComment()"
      [canEdit]="canCreateComment()"
      [canCreate]="canCreateComment()"></app-comments-list>
  `,
  imports: [CommentsListComponent],
})
export class TaskDetailCommentsComponent {
  commentsService = inject(CommentsService);
  confirmation = inject(ConfirmationService);
  snackbar = inject(SnackbarService);

  user = inject(SessionService).currentUser;
  workspaceUsers = workspaceUsersResource();

  private readonly taskDetail = inject(TaskDetailService);

  task = this.taskDetail.task;

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

  canCreateComment = selectCanCreateComment();
  canDeleteComment = selectCanDeleteComment();

  constructor() {
    reloadOnRefresh(this.comments, ['comments']);
  }

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
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Comment deleted`
          );
          this.comments.reload();
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }

  onUpdateComment(event: CommentUpdateEvent) {
    const request: UpdateCommentRequest = {
      comment: event.text,
      mentions: event.mentions,
    };

    this.commentsService
      .update(event.comment.id, request)
      .pipe(
        unwrapClientReposne(),
        tap(() => {
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Comment updated`
          );
          this.comments.reload();
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }
}

const DELETE_COMMENT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Delete`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a dialog or validation message:Are you sure you want to delete this comment?`,
  title: $localize`:Title of a dialog or section:Delete Comment`,
  color: 'warn',
};
