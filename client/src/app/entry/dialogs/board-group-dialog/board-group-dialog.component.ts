import {
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnInit,
} from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import { Store } from '@ngrx/store';

export interface BoardGroupDialogData {
  boardId: number;
  identifier: string;
}

@Component({
  selector: 'app-board-group-dialog',
  templateUrl: './board-group-dialog.component.html',
  styleUrls: ['./board-group-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupDialogComponent implements OnInit {
  formGroup!: FormGroup;

  get group() {
    return this.formGroup.get('group');
  }

  constructor(
    private store: Store,
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<BoardGroupDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: BoardGroupDialogData
  ) {}

  ngOnInit() {
    this.formGroup = this.fb.group({
      group: [],
    });
  }

  onSubmit() {
    const name = this.group?.value;
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
