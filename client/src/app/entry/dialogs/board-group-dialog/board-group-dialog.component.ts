import { ChangeDetectionStrategy, Component, Inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import {
  MatDialogRef,
  MAT_DIALOG_DATA,
} from '@angular/material/dialog';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import { Store } from '@ngrx/store';

export interface BoardGroupDialogData {
  boardId: number;
  identifier: string;
}

@Component({
  selector: 'app-board-group-dialog',
  templateUrl: './board-group-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupDialogComponent {
  form = this.fb.nonNullable.group({
    group: '',
  });

  constructor(
    private store: Store,
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<BoardGroupDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: BoardGroupDialogData
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
