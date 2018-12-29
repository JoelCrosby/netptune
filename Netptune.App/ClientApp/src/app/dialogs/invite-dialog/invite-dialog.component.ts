import { Component, OnInit, Optional, Inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AppUser } from '../../models/appuser';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material';

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

  async getResult(): Promise<AppUser> {

    const email = this.inviteFromGroup.get('emailFormControl').value;
    if (!email) { return null; }

    return email;
  }

}
