import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  signal,
} from '@angular/core';
import { UserResponse } from '@core/auth/store/auth.models';
import { CommentViewModel } from '@core/models/comment';

import { Field, form, required } from '@angular/forms/signals';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import {
  MatMenu,
  MatMenuContent,
  MatMenuItem,
  MatMenuTrigger,
} from '@angular/material/menu';
import { FromNowPipe } from '../../pipes/from-now.pipe';
import { AvatarComponent } from '../avatar/avatar.component';
import { FormInputComponent } from '../form-input/form-input.component';

@Component({
  selector: 'app-comments-list',
  templateUrl: './comments-list.component.html',
  styleUrls: ['./comments-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AvatarComponent,
    FormInputComponent,
    MatIconButton,
    MatMenuTrigger,
    MatIcon,
    MatMenu,
    MatMenuContent,
    MatMenuItem,
    FromNowPipe,
    Field,
  ],
})
export class CommentsListComponent {
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
