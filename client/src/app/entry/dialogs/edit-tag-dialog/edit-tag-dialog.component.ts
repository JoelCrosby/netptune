import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { apply, FormField, form, submit } from '@angular/forms/signals';
import { Tag } from '@core/models/tag';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

export interface EditTagDialogResult {
  name: string;
}

@Component({
  selector: 'app-edit-tag-dialog',
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Edit Tag</app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <app-form-input [formField]="tagForm.name" label="Name" maxLength="128" />
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">Close</button>
      <button app-flat-button type="button" (click)="submit($event)">
        Save Tag
      </button>
    </div>`,
})
export class EditTagDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<EditTagDialogResult, EditTagDialogComponent>>(DialogRef);

  readonly data = inject<Tag>(DIALOG_DATA);

  readonly tagFormModel = signal({
    name: this.data.name,
  });

  readonly tagForm = form(this.tagFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({ label: 'Name', maxLength: 128, minLength: 2 })
    );
  });

  submit(event: Event) {
    event.preventDefault();

    submit(this.tagForm, async () => {
      const name = this.tagForm.name().value().trim();

      this.dialogRef.close({ name });
    });
  }
}
