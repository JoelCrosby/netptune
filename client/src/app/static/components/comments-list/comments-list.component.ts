import {
  Component,
  inject,
  input,
  output,
  SecurityContext,
  signal,
} from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { UserResponse } from '@app/core/store/auth/auth.models';
import { CommentViewModel } from '@core/models/comment';
import { AppUser } from '@core/models/appuser';

import {
  LucideEllipsis,
  LucidePencil,
  LucideTrash2,
  LucideMessageSquare,
} from '@lucide/angular';
import { IconButtonComponent } from '../button/icon-button.component';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '../dropdown-menu/menu-item.component';
import { FromNowPipe } from '../../pipes/from-now.pipe';
import { AvatarComponent } from '../avatar/avatar.component';
import { FlatButtonComponent } from '../button/flat-button.component';
import { StrokedButtonComponent } from '../button/stroked-button.component';
import {
  MentionInputComponent,
  MentionSubmitEvent,
} from '../mention-input/mention-input.component';

export interface CommentSubmitEvent {
  text: string;
  mentions: string[];
}

export interface CommentUpdateEvent extends CommentSubmitEvent {
  comment: CommentViewModel;
}

@Component({
  selector: 'app-comments-list',
  imports: [
    AvatarComponent,
    MentionInputComponent,
    IconButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideEllipsis,
    LucidePencil,
    LucideTrash2,
    FlatButtonComponent,
    StrokedButtonComponent,
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
              [imageUrl]="comment.userDisplayImage"
              [isServiceAccount]="comment.userIsServiceAccount ?? false">
            </app-avatar>

            <div class="flex flex-1 flex-col">
              <span class="mb-1 flex flex-row items-center font-medium">
                {{ comment.userDisplayName }}
                <small class="ml-[0.6rem] flex-1 opacity-60">
                  {{ comment.createdAt | fromNow }}
                  @if (comment.isEdited) {
                    <span> · edited</span>
                  }
                </small>
              </span>
              @if (editingCommentId() === comment.id) {
                <app-mention-input
                  #editInput
                  class="w-full"
                  [(value)]="editText"
                  [users]="workspaceUsers()"
                  [initialMentionIds]="editMentionIds()"
                  [clearOnSubmit]="false"
                  placeholder="Edit comment — type @ to mention"
                  (mentionSubmit)="onEditSubmit(comment, $event)">
                  <div class="flex justify-end gap-2">
                    <button
                      app-stroked-button
                      type="button"
                      (click)="cancelEditing()">
                      Cancel
                    </button>
                    <button
                      app-flat-button
                      type="button"
                      [disabled]="!editText().trim()"
                      (click)="editInput.submit()">
                      Save
                    </button>
                  </div>
                </app-mention-input>
              } @else {
                <span
                  class="text-sm font-normal"
                  [innerHTML]="renderBody(comment.body)">
                </span>
              }
            </div>

            @if (canEditComment(comment) || canDeleteComment(comment)) {
              <div class="hidden w-10 group-hover:block">
                <button
                  app-icon-button
                  aria-label="Comment Actions"
                  (click)="commentMenu.toggle($any($event.currentTarget))">
                  <svg lucideEllipsis class="h-4 w-4"></svg>
                </button>
                <app-dropdown-menu #commentMenu xPosition="before">
                  @if (canEditComment(comment)) {
                    <button
                      app-menu-item
                      (click)="startEditing(comment); commentMenu.close()">
                      <svg lucidePencil class="h-4 w-4"></svg>
                      <span>Edit Comment</span>
                    </button>
                  }
                  @if (canDeleteComment(comment)) {
                    <button
                      app-menu-item
                      (click)="
                        deleteComment.emit(comment); commentMenu.close()
                      ">
                      <svg lucideTrash2 class="h-4 w-4"></svg>
                      <span>Delete Comment</span>
                    </button>
                  }
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
  readonly canEdit = input<boolean>(false);

  readonly deleteComment = output<CommentViewModel>();
  readonly commentSubmit = output<CommentSubmitEvent>();
  readonly updateComment = output<CommentUpdateEvent>();
  readonly lucideMessageSquare = LucideMessageSquare;
  readonly editingCommentId = signal<number | null>(null);
  readonly editText = signal('');
  readonly editMentionIds = signal<string[]>([]);

  onMentionSubmit(event: MentionSubmitEvent) {
    this.commentSubmit.emit({ text: event.text, mentions: event.mentions });
  }

  startEditing(comment: CommentViewModel) {
    this.editingCommentId.set(comment.id);
    this.editText.set(comment.body);
    this.editMentionIds.set(comment.mentions.map((mention) => mention.userId));
  }

  cancelEditing() {
    this.editingCommentId.set(null);
    this.editText.set('');
    this.editMentionIds.set([]);
  }

  onEditSubmit(comment: CommentViewModel, event: MentionSubmitEvent) {
    this.updateComment.emit({
      comment,
      text: event.text,
      mentions: event.mentions,
    });
    this.cancelEditing();
  }

  canEditComment(comment: CommentViewModel) {
    return this.canEdit() && comment.userId === this.user()?.userId;
  }

  canDeleteComment(comment: CommentViewModel) {
    return (
      this.canDeleteAny() ||
      (this.canDelete() && comment.userId === this.user()?.userId)
    );
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
