import {
  ChangeDetectionStrategy,
  Component,
  Input,
  input,
  output
} from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { UserResponse } from '@core/auth/store/auth.models';
import { CommentViewModel } from '@core/models/comment';

import { AvatarComponent } from '../avatar/avatar.component';
import { FormInputComponent } from '../form-input/form-input.component';
import { MatIconButton } from '@angular/material/button';
import { MatMenuTrigger, MatMenu, MatMenuContent, MatMenuItem } from '@angular/material/menu';
import { MatIcon } from '@angular/material/icon';
import { FromNowPipe } from '../../pipes/from-now.pipe';

@Component({
    selector: 'app-comments-list',
    templateUrl: './comments-list.component.html',
    styleUrls: ['./comments-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AvatarComponent, FormsModule, ReactiveFormsModule, FormInputComponent, MatIconButton, MatMenuTrigger, MatIcon, MatMenu, MatMenuContent, MatMenuItem, FromNowPipe]
})
export class CommentsListComponent {
  @Input() user!: UserResponse | null | undefined;
  readonly comments = input.required<CommentViewModel[] | null>();

  readonly deleteComment = output<CommentViewModel>();
  readonly commentSubmit = output<string>();

  formGroup = new FormGroup({
    comment: new FormControl(''),
  });

  get comment() {
    return this.formGroup.controls.comment;
  }

  get value() {
    return this.comment.value as string;
  }

  submit() {
    this.commentSubmit.emit(this.value);
    this.comment.reset();
  }
}
