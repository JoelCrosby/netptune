import { httpResource } from '@angular/common/http';
import { computed, inject, Injectable } from '@angular/core';
import { DEFAULT_PAGE_SIZE } from '@core/models/pagination';
import { CommentViewModel } from '@core/models/comment';
import {
  AddCommentRequest,
  UpdateCommentRequest,
} from '@core/models/requests/add-comment-request';
import { CommentsService } from '@core/services/comments.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { reloadOnRefresh } from '@core/util/reload-on-refresh';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  CommentReactionEvent,
  CommentSubmitEvent,
  CommentUpdateEvent,
} from '@static/components/comments-list/comments-list.component';
import { EMPTY } from 'rxjs';
import { catchError, filter, switchMap, tap } from 'rxjs/operators';
import { ConfirmDialogOptions } from '../../confirm-dialog/confirm-dialog.component';
import { TaskDetailService } from '../task-detail.service';

@Injectable()
export class TaskDetailCommentsService {
  private readonly commentsService = inject(CommentsService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly taskDetail = inject(TaskDetailService);

  readonly resource = httpResource<CommentViewModel[]>(
    () => {
      const systemId = this.taskDetail.task()?.systemId;

      if (!systemId) return undefined;

      return {
        url: `api/comments/task/${systemId}`,
        params: { page: 1, pageSize: DEFAULT_PAGE_SIZE },
      };
    },
    { defaultValue: [] }
  );

  readonly comments = this.resource.value;
  readonly count = computed(() => this.comments().length);
  readonly latest = computed(() => this.comments().at(0) ?? null);

  constructor() {
    reloadOnRefresh(this.resource, ['comments']);
  }

  submit(event: CommentSubmitEvent) {
    const task = this.taskDetail.task();

    if (!event.text || !task) return;

    const request: AddCommentRequest = {
      comment: event.text,
      systemId: task.systemId,
      mentions: event.mentions,
    };

    this.commentsService
      .postToTask(request)
      .pipe(
        unwrapClientResponse(),
        tap(() => this.resource.reload()),
        catchError(() => EMPTY)
      )
      .subscribe();
  }

  update(event: CommentUpdateEvent) {
    const request: UpdateCommentRequest = {
      comment: event.text,
      mentions: event.mentions,
    };

    this.commentsService
      .update(event.comment.id, request)
      .pipe(
        unwrapClientResponse(),
        tap(() => {
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Comment updated`
          );
          this.resource.reload();
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }

  confirmDelete(comment: CommentViewModel) {
    this.confirmation
      .open(DELETE_COMMENT_CONFIRMATION)
      .pipe(
        filter(Boolean),
        switchMap(() => this.commentsService.delete(comment.id)),
        unwrapClientResponse(),
        tap(() => {
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Comment deleted`
          );
          this.resource.reload();
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }

  toggleReaction(event: CommentReactionEvent) {
    const request = event.reacted
      ? this.commentsService.removeReaction(event.comment.id, event.value)
      : this.commentsService.addReaction(event.comment.id, event.value);

    request
      .pipe(
        unwrapClientResponse(),
        tap((comment) => this.replaceComment(comment)),
        catchError(() => {
          this.resource.reload();

          return EMPTY;
        })
      )
      .subscribe();
  }

  private replaceComment(updated: CommentViewModel) {
    this.resource.update((comments) => {
      return comments.map((comment) => {
        return comment.id === updated.id ? updated : comment;
      });
    });
  }
}

const DELETE_COMMENT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Delete`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a dialog or validation message:Are you sure you want to delete this comment?`,
  title: $localize`:Title of a dialog or section:Delete Comment`,
  color: 'warn',
};
