import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';

@Component({
  selector: 'app-invite-dialog',
  templateUrl: './invite-dialog.component.html',
  styleUrls: ['./invite-dialog.component.scss']
})
export class InviteDialogComponent implements OnInit {

  inviteFromGroup = new FormGroup({

    emailFormControl: new FormControl('', [
      Validators.required,
      Validators.email
    ]),

  });

  constructor(
    public dialogRef: MatDialogRef<InviteDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: string) { }

  ngOnInit() {
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {

    const email = this.inviteFromGroup.get('emailFormControl');
    if (!email) { return null; }

    this.dialogRef.close(email.value || undefined);
  }

}
