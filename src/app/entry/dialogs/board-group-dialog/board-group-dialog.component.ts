import {
  Component,
  OnInit,
  ChangeDetectionStrategy,
  Input,
  Inject,
} from '@angular/core';
import { FormControl } from '@angular/forms';
import { AppState } from '@app/core/core.state';
import { Store } from '@ngrx/store';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

export interface BoardGroupDialogData {
  boardId: number;
}

@Component({
  selector: 'app-board-group-dialog',
  templateUrl: './board-group-dialog.component.html',
  styleUrls: ['./board-group-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupDialogComponent implements OnInit {
  groupFormControl = new FormControl();

  constructor(
    private store: Store<AppState>,
    public dialogRef: MatDialogRef<BoardGroupDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: BoardGroupDialogData
  ) {}

  ngOnInit() {}

  onSubmit() {
    const name = this.groupFormControl.value;

    this.store.dispatch(
      BoardGroupActions.createBoardGroup({
        request: {
          name,
          boardId: this.data.boardId,
        },
      })
    );

    this.dialogRef.close();
  }
}
