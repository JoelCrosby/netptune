import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Post } from '@core/models/post';

@Component({
  selector: 'app-board-post-dialog',
  templateUrl: './board-post-dialog.component.html',
  styleUrls: ['./board-post-dialog.component.scss'],
})
export class BoardPostDialogComponent implements OnInit {
  postFromGroup = new FormGroup({
    titleFormControl: new FormControl('', [Validators.required]),
  });

  constructor(
    public dialogRef: MatDialogRef<BoardPostDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Post
  ) {}

  ngOnInit() {}

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    const title = this.postFromGroup.get('titleFormControl');
    if (!title) {
      return null;
    }

    const value = title.value;
    const post = {
      title: value,
    };

    this.dialogRef.close(post);
  }
}
