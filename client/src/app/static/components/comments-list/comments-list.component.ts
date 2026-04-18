import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  signal,
} from '@angular/core';
import { UserResponse } from '@core/auth/store/auth.models';
import { CommentViewModel } from '@core/models/comment';

import { FormField, form, required } from '@angular/forms/signals';
import {
  LucideEllipsis,
  LucideMessageCircle,
  LucideTrash2,
} from '@lucide/angular';
import { IconButtonComponent } from '../button/icon-button.component';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '../dropdown-menu/menu-item.component';
import { FromNowPipe } from '../../pipes/from-now.pipe';
import { AvatarComponent } from '../avatar/avatar.component';
import { FormInputComponent } from '../form-input/form-input.component';

@Component({
  selector: 'app-comments-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AvatarComponent,
    FormInputComponent,
    IconButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideEllipsis,
    LucideTrash2,
    FromNowPipe,
    FormField,
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
          <form class="flex-1" (submit)="submit($event)">
            <app-form-input
              [formField]="commentForm.comment"
              placeholder="Add Comment"
              [icon]="lucideMessageCircle"
              [noMargin]="true">
            </app-form-input>
          </form>
        </div>
      }
      <div class="mb-4 flex flex-col" [class.ml-12]="canCreate()">
        @for (comment of comments(); track comment.id) {
          <div
            class="group mb-1 flex min-h-12 flex-row items-center gap-4 rounded-md p-2 hover:bg-neutral-50 dark:hover:bg-neutral-800">
            <app-avatar
              size="md"
              class=""
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
              <span class="text-sm font-normal">
                {{ comment.body }}
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
  lucideMessageCircle = LucideMessageCircle;
  readonly user = input<UserResponse>();
  readonly comments = input.required<CommentViewModel[] | null>();
  readonly canDelete = input<boolean>(false);
  readonly canDeleteAny = input<boolean>(false);
  readonly canCreate = input<boolean>(false);

  readonly deleteComment = output<CommentViewModel>();
  readonly commentSubmit = output<string>();

  commentFormModel = signal({
    comment: '',
  });

  commentForm = form(this.commentFormModel, (schema) => {
    required(schema.comment);
  });

  submit(event: Event) {
    event.preventDefault();

    this.commentSubmit.emit(this.commentForm.comment().value());
    this.commentForm.comment().value.set('');
    this.commentForm().reset();
  }
}
