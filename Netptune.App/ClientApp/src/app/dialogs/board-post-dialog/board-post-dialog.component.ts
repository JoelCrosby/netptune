import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Post } from '../../models/post';

@Component({
  selector: 'app-board-post-dialog',
  templateUrl: './board-post-dialog.component.html',
  styleUrls: ['./board-post-dialog.component.scss']
})
export class BoardPostDialogComponent implements OnInit {

  postFromGroup = new FormGroup({

    titleFormControl: new FormControl('', [
      Validators.required,
    ]),

  });

  constructor(
    public dialogRef: MatDialogRef<BoardPostDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Post) { }

  ngOnInit() {
  }

  close(): void {
    this.dialogRef.close();
  }

  async getResult(): Promise<Post> {

    const title = this.postFromGroup.get('titleFormControl').value;
    if (!title) { return null; }

    const post = new Post();
    post.title = title;

    return post;
  }

}
