import { Component, OnInit, Optional, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';

@Component({
  selector: 'app-confirm-dialog',
  templateUrl: './confirm-dialog.component.html',
  styleUrls: ['./confirm-dialog.component.scss']
})
export class ConfirmDialogComponent implements OnInit {

  constructor(
    public dialogRef: MatDialogRef<ConfirmDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: ConfirmDialogOptions) {

  }

  ngOnInit() {
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    return true;
  }

}

export interface ConfirmDialogOptions {
  title?: string;
  content?: string;
  confirm?: string;
}
