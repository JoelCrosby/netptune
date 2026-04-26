import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  SecurityContext,
} from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { inject } from '@angular/core';
import { UserResponse } from '@core/auth/store/auth.models';
import { CommentViewModel } from '@core/models/comment';
import { AppUser } from '@core/models/appuser';

import {
  LucideEllipsis,
  LucideTrash2,
  LucideMessageSquare,
} from '@lucide/angular';
import { IconButtonComponent } from '../button/icon-button.component';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '../dropdown-menu/menu-item.component';
import { FromNowPipe } from '../../pipes/from-now.pipe';
import { AvatarComponent } from '../avatar/avatar.component';
import {
  MentionInputComponent,
  MentionSubmitEvent,
} from '../mention-input/mention-input.component';

export interface CommentSubmitEvent {
  text: string;
  mentions: string[];
}

@Component({
  selector: 'app-comments-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AvatarComponent,
    MentionInputComponent,
    IconButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideEllipsis,
    LucideTrash2,
    FromNowPipe,
  ],
  template: `
    <div>
      @if (canCreate() && user(); as user) {
        <div class="my-4 flex flex-row items-center gap-4">
          <app-avatar
            size="lg"
            [name]="user.displayName"
            [imageUrl]="user.pictureUrl">
          </app-avatar>
          <app-mention-input
            class="flex-1"
            [users]="workspaceUsers()"
            (mentionSubmit)="onMentionSubmit($event)"
            [icon]="lucideMessageSquare">
          </app-mention-input>
        </div>
      }
      <div class="mb-4 flex flex-col" [class.ml-12]="canCreate()">
        @for (comment of comments(); track comment.id) {
          <div
            class="group mb-1 flex min-h-12 flex-row items-center gap-4 rounded-md p-2 hover:bg-neutral-50 dark:hover:bg-neutral-800">
            <app-avatar
              size="md"
              [name]="comment.userDisplayName"
              [imageUrl]="comment.userDisplayImage">
            </app-avatar>

            <div class="flex flex-1 flex-col">
              <span class="mb-1 flex flex-row items-center font-medium">
                {{ comment.userDisplayName }}
                <small class="ml-[0.6rem] flex-1 opacity-60">
                  {{ comment.createdAt | fromNow }}
                </small>
              </span>
              <span
                class="text-sm font-normal"
                [innerHTML]="renderBody(comment.body)">
              </span>
            </div>

            @if (
              canDeleteAny() ||
              (canDelete() &&
                (comment.userId === user()?.userId || canDeleteAny()))
            ) {
              <div class="hidden w-10 group-hover:block">
                <button
                  app-icon-button
                  aria-label="Comment Actions"
                  (click)="commentMenu.toggle($any($event.currentTarget))">
                  <svg lucideEllipsis class="h-4 w-4"></svg>
                </button>
                <app-dropdown-menu #commentMenu xPosition="before">
                  <button
                    app-menu-item
                    (click)="deleteComment.emit(comment); commentMenu.close()">
                    <svg lucideTrash2 class="h-4 w-4"></svg>
                    <span>Delete Comment</span>
                  </button>
                </app-dropdown-menu>
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class CommentsListComponent {
  private sanitizer = inject(DomSanitizer);

  readonly user = input<UserResponse>();
  readonly comments = input.required<CommentViewModel[] | null>();
  readonly workspaceUsers = input<AppUser[] | null>([]);
  readonly canDelete = input<boolean>(false);
  readonly canDeleteAny = input<boolean>(false);
  readonly canCreate = input<boolean>(false);

  readonly deleteComment = output<CommentViewModel>();
  readonly commentSubmit = output<CommentSubmitEvent>();
  readonly lucideMessageSquare = LucideMessageSquare;

  onMentionSubmit(event: MentionSubmitEvent) {
    this.commentSubmit.emit({ text: event.text, mentions: event.mentions });
  }

  renderBody(body: string): string {
    const escaped = body
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');

    const withMentions = escaped.replace(
      /@([\w][\w.\- ]*[\w]|[\w]+)/g,
      '<span class="text-primary font-medium">@$1</span>'
    );

    return this.sanitizer.sanitize(SecurityContext.HTML, withMentions) ?? '';
  }
}
