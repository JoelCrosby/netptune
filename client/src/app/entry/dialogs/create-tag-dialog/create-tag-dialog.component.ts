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
  template: `
    <app-dialog-title i18n="Title of the create-tag dialog">
      Create Tag
    </app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <app-form-input
        [formField]="tagForm.name"
        i18n-label="Label of the name field"
        label="Name"
        maxLength="128" />
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without saving">Close</span>
      </button>
      <button app-flat-button type="button" (click)="submit($event)">
        <span i18n="Button that creates the tag">Create Tag</span>
      </button>
    </div>
  `,
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
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Name is required.":Name`,
        maxLength: 128,
        minLength: 2,
      })
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
