import { Component, computed, inject, input } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { workspaceUsersResource } from '@core/resources/user.resource';
import { SessionService } from '@core/services/session.service';
import { LucideMessageSquare } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { MentionInputComponent } from '@static/components/mention-input/mention-input.component';
import { TaskDetailCommentsService } from './task-detail-comments.service';

@Component({
  selector: 'app-task-detail-composer',
  imports: [AvatarComponent, MentionInputComponent],
  host: { class: 'flex items-center gap-3' },
  template: `
    @if (canCreate() && user(); as user) {
      @if (showAvatar()) {
        <app-avatar
          size="sm"
          [tooltip]="false"
          [name]="user.displayName"
          [imageUrl]="user.pictureUrl" />
      }

      <app-mention-input
        class="min-w-0 flex-1"
        density="compact"
        [fieldClass]="fieldClass()"
        [users]="workspaceUsers()"
        [placeholder]="placeholder()"
        [icon]="messageIcon"
        (mentionSubmit)="comments.submit($event)" />
    }
  `,
})
export class TaskDetailComposerComponent {
  readonly showAvatar = input(true);

  readonly shape = input<'field' | 'pill'>('field');

  readonly comments = inject(TaskDetailCommentsService);

  readonly user = inject(SessionService).currentUser;
  readonly workspaceUsers = workspaceUsersResource();
  readonly canCreate = hasPermission(PERMISSIONS.comments.create);
  readonly messageIcon = LucideMessageSquare;

  readonly placeholder = input(
    $localize`:Placeholder in the task comment box. The @ character triggers the mention picker and must be kept:Comment — @ to mention`
  );

  readonly fieldClass = computed(() => {
    return this.shape() === 'pill' ? 'h-10 rounded-full' : 'h-10 rounded-lg';
  });
}
