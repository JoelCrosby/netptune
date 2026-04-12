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
  templateUrl: './comments-list.component.html',
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
})
export class CommentsListComponent {
  lucideMessageCircle = LucideMessageCircle;
  readonly user = input.required<UserResponse>();
  readonly comments = input.required<CommentViewModel[] | null>();

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
