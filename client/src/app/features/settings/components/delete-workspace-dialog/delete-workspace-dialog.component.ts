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
  template: `
    <app-dialog-title i18n="Title of the delete-workspace dialog">
      Delete Workspace
    </app-dialog-title>

    <form (submit)="confirmDelete($event)">
      <app-dialog-content>
        <p class="text-foreground/80 mb-4 text-sm">
          <span
            i18n="
              Warning before deleting a workspace. NAME is the workspace name
            ">
            This will delete
            <strong>{{ workspace.name }}</strong>
            for every member. To confirm, type the workspace name below.
          </span>
        </p>

        <app-form-input
          [formField]="confirmationForm.workspaceName"
          i18n-label="Label of the field confirming the workspace name"
          label="Workspace name"
          autocomplete="off" />
      </app-dialog-content>

      <div app-dialog-actions align="end">
        <button app-stroked-button app-dialog-close type="button">
          <span i18n="Dismisses a dialog without acting">Cancel</span>
        </button>
        <button
          app-flat-button
          color="warn"
          type="submit"
          [disabled]="confirmationForm().invalid()">
          <span i18n="Button that permanently deletes the workspace">
            Delete Workspace
          </span>
        </button>
      </div>
    </form>
  `,
})
export class DeleteWorkspaceDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<boolean, DeleteWorkspaceDialogComponent>>(DialogRef);

  readonly workspace = inject<Workspace>(DIALOG_DATA);
  readonly confirmationModel = signal({ workspaceName: '' });
  readonly confirmationForm = form(this.confirmationModel, (schema) => {
    required(schema.workspaceName, {
      message: $localize`:Validation error when the workspace name confirmation is empty:Enter the workspace name to continue.`,
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
