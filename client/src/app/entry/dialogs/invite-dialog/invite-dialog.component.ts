import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-invite-dialog',
  templateUrl: './invite-dialog.component.html',
  styleUrls: ['./invite-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InviteDialogComponent {
  users: string[] = [];

  get email() {
    return this.formGroup.get('email') as FormControl;
  }

  formGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
  });

  constructor(private dialogRef: MatDialogRef<InviteDialogComponent>) {}

  close() {
    this.dialogRef.close();
  }

  getResult() {
    this.dialogRef.close(this.users);
  }

  add() {
    if (!this.email.valid) {
      this.email.markAsDirty();
      return;
    }

    const user: string = this.email.value;

    if (this.users.includes(user)) {
      this.email.reset();
      return;
    }

    this.users.push(user);
    this.email.reset();
  }

  remove(user: string): void {
    const index = this.users.indexOf(user);

    if (index >= 0) {
      this.users.splice(index, 1);
    }
  }

  getErrorMessage() {
    if (this.email.hasError('required')) {
      return 'You must enter a value';
    }

    return this.email.hasError('email') ? 'Not a valid email' : '';
  }
}
