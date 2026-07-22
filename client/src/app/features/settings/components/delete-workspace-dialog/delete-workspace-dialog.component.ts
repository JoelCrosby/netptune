import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import {
  FormField,
  form,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { Workspace } from '@core/models/workspace';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  selector: 'app-delete-workspace-dialog',
  imports: [
    DialogTitleComponent,
    DialogContentComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormInputComponent,
    FormField,
  ],
  template: `<app-dialog-title>Delete Workspace</app-dialog-title>

    <form (submit)="confirmDelete($event)">
      <app-dialog-content>
        <p class="text-foreground/80 mb-4 text-sm">
          This will delete <strong>{{ workspace.name }}</strong> for every
          member. To confirm, type the workspace name below.
        </p>

        <app-form-input
          [formField]="confirmationForm.workspaceName"
          label="Workspace name"
          autocomplete="off" />
      </app-dialog-content>

      <div app-dialog-actions align="end">
        <button app-stroked-button app-dialog-close type="button">
          Cancel
        </button>
        <button
          app-flat-button
          color="warn"
          type="submit"
          [disabled]="confirmationForm().invalid()">
          Delete Workspace
        </button>
      </div>
    </form>`,
})
export class DeleteWorkspaceDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<boolean, DeleteWorkspaceDialogComponent>>(DialogRef);

  readonly workspace = inject<Workspace>(DIALOG_DATA);
  readonly confirmationModel = signal({ workspaceName: '' });
  readonly confirmationForm = form(this.confirmationModel, (schema) => {
    required(schema.workspaceName, {
      message: 'Enter the workspace name to continue.',
    });
    maxLength(schema.workspaceName, 1024);
    validate(schema.workspaceName, ({ value }) => {
      const workspaceName = value();

      if (!workspaceName || workspaceName === this.workspace.name) {
        return undefined;
      }

      return {
        kind: 'workspaceNameMismatch',
        message: `Enter ${this.workspace.name} exactly as shown.`,
      };
    });
  });

  confirmDelete(event: Event) {
    event.preventDefault();

    submit(this.confirmationForm, async () => {
      this.dialogRef.close(true);
    });
  }
}
