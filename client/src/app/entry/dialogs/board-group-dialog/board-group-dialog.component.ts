import { ChangeDetectionStrategy, Component, Inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DialogRef, DIALOG_DATA } from '@angular/cdk/dialog';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { MatButton } from '@angular/material/button';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

export interface BoardGroupDialogData {
  boardId: number;
  identifier: string;
}

@Component({
  selector: 'app-board-group-dialog',
  templateUrl: './board-group-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    DialogActionsDirective,
    MatButton,
    DialogCloseDirective,
  ],
})
export class BoardGroupDialogComponent {
  form = this.fb.nonNullable.group({
    group: '',
  });

  constructor(
    private store: Store,
    private fb: FormBuilder,
    public dialogRef: DialogRef<BoardGroupDialogComponent>,
    @Inject(DIALOG_DATA) public data: BoardGroupDialogData
  ) {}

  onSubmit() {
    const name = this.form.getRawValue().group;
    const identifier = this.data.identifier;

    this.store.dispatch(
      BoardGroupActions.createBoardGroup({
        identifier,
        request: {
          name,
          boardId: this.data.boardId,
        },
      })
    );

    this.dialogRef.close();
  }
}
