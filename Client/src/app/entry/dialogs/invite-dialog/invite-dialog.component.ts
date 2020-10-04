import {
  Component,
  Inject,
  OnInit,
  Optional,
  ChangeDetectionStrategy,
} from '@angular/core';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatChipInputEvent } from '@angular/material/chips';

@Component({
  selector: 'app-invite-dialog',
  templateUrl: './invite-dialog.component.html',
  styleUrls: ['./invite-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InviteDialogComponent implements OnInit {
  visible = true;
  selectable = true;
  removable = true;
  addOnBlur = true;

  readonly separatorKeysCodes: number[] = [ENTER, COMMA];

  users: string[] = [];

  get email() {
    return this.inviteFromGroup.get('email');
  }

  inviteFromGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
  });

  constructor(
    public dialogRef: MatDialogRef<InviteDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: string
  ) {}

  ngOnInit() {}

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    this.dialogRef.close(this.users);
  }

  add(event: MatChipInputEvent): void {
    if (!this.email.valid) {
      this.email.markAsDirty();
      return;
    }

    const value = event.value;

    if ((value || '').trim()) {
      this.users.push(value.trim());
    }

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
