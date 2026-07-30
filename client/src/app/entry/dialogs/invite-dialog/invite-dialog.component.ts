import { DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

import {
  email,
  FormField,
  form,
  maxLength,
  required,
  submit,
} from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { LucideUserRoundPlus } from '@lucide/angular';

@Component({
  selector: 'app-invite-dialog',
  imports: [
    DialogTitleComponent,
    DialogContentComponent,
    FormInputComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormField,
  ],
  template: `
    <app-dialog-title
      i18n="Title of the dialog for inviting people to a workspace">
      Invite Users to Workspace
    </app-dialog-title>

    <app-dialog-content>
      <form (submit)="add($event)">
        <app-form-input
          [formField]="inviteForm.email"
          i18n-label="Label of the field for an invitee's e-mail address"
          label="Invitee Email"
          maxLength="128"
          type="email"
          [icon]="lucideUserRoundPlus"></app-form-input>

        <div class="max-h-124 min-h-32 overflow-y-auto">
          @for (user of users(); track user) {
            <div class="border-border rounded-sm border px-4 py-2">
              {{ user }}
            </div>
          } @empty {
            <div class="app-list-message">
              <span i18n="Empty state of the pending invitee list">
                Email addresses entered below will show here.
              </span>
            </div>
          }
        </div>
      </form>
    </app-dialog-content>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">
        <span i18n="Dismisses a dialog without saving">Close</span>
      </button>
      <button
        app-flat-button
        type="button"
        [disabled]="users().length === 0"
        (click)="getResult()">
        <span i18n="Button that sends the workspace invitations">
          Invite Users
        </span>
      </button>
    </div>
  `,
})
export class InviteDialogComponent {
  lucideUserRoundPlus = LucideUserRoundPlus;

  private dialogRef =
    inject<DialogRef<string[], InviteDialogComponent>>(DialogRef);

  users = signal<string[]>([]);

  inviteFormModel = signal({
    email: '',
  });

  inviteForm = form(this.inviteFormModel, (schema) => {
    required(schema.email, {
      message: $localize`:Body of a dialog or validation message:Email is required.`,
    });
    email(schema.email, {
      message: $localize`:Body of a dialog or validation message:Enter a valid email address.`,
    });
    maxLength(schema.email, 128);
  });

  close() {
    this.dialogRef.close();
  }

  getResult() {
    if (this.users().length === 0) return;

    this.dialogRef.close(this.users());
  }

  add(event: Event) {
    event.preventDefault();

    submit(this.inviteForm, async () => {
      const user = this.inviteForm.email().value().trim().toLowerCase();

      if (!this.users().includes(user)) {
        this.users.update((users) => [...users, user]);
      }

      this.inviteForm.email().value.set('');
      this.inviteForm.email().reset();
    });
  }

  remove(user: string): void {
    const index = this.users().indexOf(user);

    if (index >= 0) {
      this.users.update((users) => users.filter((item) => item !== user));
    }
  }
}
