import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

import { email, Field, form, required } from '@angular/forms/signals';
import { MatButton } from '@angular/material/button';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

@Component({
  selector: 'app-invite-dialog',
  templateUrl: './invite-dialog.component.html',
  styleUrls: ['./invite-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DialogContentComponent,
    FormInputComponent,
    DialogActionsDirective,
    MatButton,
    Field,
  ],
})
export class InviteDialogComponent {
  private dialogRef =
    inject<DialogRef<string[], InviteDialogComponent>>(DialogRef);

  users = signal<string[]>([]);

  inviteFormModel = signal({
    email: '',
  });

  inviteForm = form(this.inviteFormModel, (schema) => {
    required(schema.email);
    email(schema.email);
  });

  close() {
    this.dialogRef.close();
  }

  getResult() {
    this.dialogRef.close(this.users());
  }

  add() {
    if (this.inviteForm().invalid()) {
      this.inviteForm().markAsDirty();
      return;
    }

    const user = this.inviteForm.email().value();

    if (this.users().includes(user)) {
      this.inviteForm.email().value.set('');
      this.inviteForm.email().reset();
      return;
    }

    this.users.update((u) => [...u, user]);
    this.inviteForm.email().value.set('');
    this.inviteForm.email().reset();
  }

  remove(user: string): void {
    const index = this.users().indexOf(user);

    if (index >= 0) {
      this.users.update((u) => u.splice(index, 1));
    }
  }
}
