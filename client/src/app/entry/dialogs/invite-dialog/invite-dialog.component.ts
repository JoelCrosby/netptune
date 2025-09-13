import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  FormControl,
  FormGroup,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { DialogRef } from '@angular/cdk/dialog';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { MatButton } from '@angular/material/button';

@Component({
  selector: 'app-invite-dialog',
  templateUrl: './invite-dialog.component.html',
  styleUrls: ['./invite-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DialogContentComponent,
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    DialogActionsDirective,
    MatButton
],
})
export class InviteDialogComponent {
  private dialogRef = inject<DialogRef<string[], InviteDialogComponent>>(DialogRef);

  users: string[] = [];

  get email() {
    return this.formGroup.controls.email;
  }

  formGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
  });

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

    const user = this.email.value as string;

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
