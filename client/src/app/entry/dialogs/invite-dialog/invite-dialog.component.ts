import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

import { email, FormField, form, required } from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

@Component({
  selector: 'app-invite-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DialogTitleComponent,
    DialogContentComponent,
    FormInputComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormField,
  ],
  template: `<app-dialog-title>Invite Users to Workspace</app-dialog-title>

    <app-dialog-content>
      <form (submit)="add($event)">
        <app-form-input
          [formField]="inviteForm.email"
          label="Invitee Email"
          maxLength="128"
          type="email">
        </app-form-input>

        <div class="max-h-[496px] min-h-[128px] overflow-y-auto">
          @for (user of users(); track user) {
            <div class="px-4">{{ user }}</div>
          } @empty {
            <div class="app-list-message">
              Email addresses entered below will show here.
            </div>
          }
        </div>
      </form>
    </app-dialog-content>

    <div app-dialog-actions align="end">
      <button app-stroked-button (click)="close()">Close</button>
      <button app-flat-button (click)="getResult()">Invite Users</button>
    </div> `,
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

  add(event: Event) {
    event.preventDefault();

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
