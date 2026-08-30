import { Component, inject, input } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { hasPermission } from '@core/auth/has-permission';
import { SessionService } from '@core/services/session.service';
import { workspaceUsersResource } from '@core/resources/user.resource';
import {
  CommentsListComponent,
  CommentsListDensity,
} from '@static/components/comments-list/comments-list.component';
import { TaskDetailCommentsService } from './shared/task-detail-comments.service';

@Component({
  selector: 'app-task-detail-comments',
  template: `
    <app-comments-list
      [user]="user()"
      [comments]="comments.comments()"
      [workspaceUsers]="workspaceUsers()"
      [density]="density()"
      [showComposer]="false"
      (updateComment)="comments.update($event)"
      (deleteComment)="comments.confirmDelete($event)"
      (toggleCommentReaction)="comments.toggleReaction($event)"
      [canDelete]="canDeleteComment()"
      [canEdit]="canCreateComment()"
      [canCreate]="canCreateComment()"
      [canReact]="canCreateComment()"></app-comments-list>
  `,
  imports: [CommentsListComponent],
})
export class TaskDetailCommentsComponent {
  readonly density = input<CommentsListDensity>('compact');

  readonly comments = inject(TaskDetailCommentsService);

  readonly user = inject(SessionService).currentUser;
  readonly workspaceUsers = workspaceUsersResource();

  readonly canCreateComment = hasPermission(PERMISSIONS.comments.create);
  readonly canDeleteComment = hasPermission(PERMISSIONS.comments.deleteOwn);
}
