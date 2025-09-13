import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { UserResponse } from '@core/auth/store/auth.models';
import { CommentViewModel } from '@core/models/comment';
import { NgIf, NgFor } from '@angular/common';
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
    imports: [NgIf, AvatarComponent, FormsModule, ReactiveFormsModule, FormInputComponent, NgFor, MatIconButton, MatMenuTrigger, MatIcon, MatMenu, MatMenuContent, MatMenuItem, FromNowPipe]
})
export class CommentsListComponent {
  @Input() user!: UserResponse | null | undefined;
  @Input() comments!: CommentViewModel[] | null;

  @Output() deleteComment = new EventEmitter<CommentViewModel>();
  @Output() commentSubmit = new EventEmitter<string>();

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
