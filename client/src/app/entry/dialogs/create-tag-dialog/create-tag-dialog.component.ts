import { DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { FormField, form, required } from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

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
    required(schema.name);
  });

  submit(event: Event) {
    event.preventDefault();

    if (this.tagForm().invalid()) {
      this.tagForm().markAsTouched();
      return;
    }

    const name = this.tagForm.name().value().trim();
    if (!name) return;

    this.dialogRef.close({ name });
  }
}
