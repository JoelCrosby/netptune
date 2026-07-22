import { DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { apply, FormField, form, submit } from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

export interface CreateTagDialogResult {
  name: string;
}

@Component({
  selector: 'app-create-tag-dialog',
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Create Tag</app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <app-form-input [formField]="tagForm.name" label="Name" maxLength="128" />
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">Close</button>
      <button app-flat-button type="button" (click)="submit($event)">
        Create Tag
      </button>
    </div>`,
})
export class CreateTagDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<CreateTagDialogResult, CreateTagDialogComponent>>(
      DialogRef
    );

  readonly tagFormModel = signal({
    name: '',
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
