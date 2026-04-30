import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import {
  selectCanCreateComment,
  selectCanDeleteComment,
  selectCanUpdateTask,
} from '@app/core/store/permissions/permissions.selectors';
import { addComment, deleteComment } from '@app/core/store/tasks/tasks.actions';
import {
  selectComments,
  selectDetailTask,
} from '@app/core/store/tasks/tasks.selectors';
import { selectCurrentUser } from '@app/core/store/auth/auth.selectors';
import { CommentViewModel } from '@core/models/comment';
import { AddCommentRequest } from '@core/models/requests/add-comment-request';
import { selectCurrentHubGroupId } from '@core/store/hub-context/hub-context.selectors';
import { selectAllUsers } from '@core/store/users/users.selectors';
import { Store } from '@ngrx/store';
import {
  CommentsListComponent,
  CommentSubmitEvent,
} from '@static/components/comments-list/comments-list.component';

@Component({
  selector: 'app-task-detail-comments',
  template: `
    <h4 class="font-sm mt-4 mb-2 font-semibold">Comments</h4>
    <app-comments-list
      [user]="user()"
      [comments]="comments()"
      [workspaceUsers]="workspaceUsers()"
      (commentSubmit)="onCommentSubmit($event)"
      (deleteComment)="onDeleteCommentClicked($event)"
      [canDelete]="canDeleteComment()"
      [canCreate]="canCreateComment()">
    </app-comments-list>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommentsListComponent],
})
export class TaskDetailCommentsComponent {
  store = inject(Store);

  comments = this.store.selectSignal(selectComments);
  user = this.store.selectSignal(selectCurrentUser);
  workspaceUsers = this.store.selectSignal(selectAllUsers);

  task = this.store.selectSignal(selectDetailTask);
  hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);
  selectedTags = computed(() => this.task()?.tags ?? []);

  canCreateComment = selectCanCreateComment(this.store);
  canDeleteComment = selectCanDeleteComment(this.store);
  canUpdateTask = selectCanUpdateTask(this.store);

  isReadOnly = computed(() => !this.canUpdateTask());

  onCommentSubmit(event: CommentSubmitEvent) {
    if (!event.text) return;

    const task = this.task();

    if (!task) return;

    const request: AddCommentRequest = {
      comment: event.text,
      systemId: task.systemId,
      mentions: event.mentions,
    };

    this.store.dispatch(addComment({ request }));
  }

  onDeleteCommentClicked(comment: CommentViewModel) {
    const commentId = comment.id;
    this.store.dispatch(deleteComment({ commentId }));
  }
}
