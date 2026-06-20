import { DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { FormField, form, required } from '@angular/forms/signals';
import {
  StatusCategory,
  statusCategoryLabels,
  statusCategoryOptions,
} from '@core/models/status';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

export interface CreateStatusDialogResult {
  name: string;
  category: StatusCategory;
}

@Component({
  selector: 'app-create-status-dialog',
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Create Status</app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <app-form-input
        [formField]="statusForm.name"
        label="Name"
        maxLength="128" />

      <app-form-select [formField]="statusForm.category" label="Category">
        @for (category of categories; track category) {
          <app-form-select-option [value]="category">
            {{ categoryLabel(category) }}
          </app-form-select-option>
        }
      </app-form-select>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">Close</button>
      <button app-flat-button type="button" (click)="submit($event)">
        Create Status
      </button>
    </div>`,
})
export class CreateStatusDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<CreateStatusDialogResult, CreateStatusDialogComponent>>(
      DialogRef
    );

  readonly categories = statusCategoryOptions;

  readonly statusFormModel = signal({
    name: '',
    category: StatusCategory.backlog,
  });

  readonly statusForm = form(this.statusFormModel, (schema) => {
    required(schema.name);
    required(schema.category);
  });

  submit(event: Event) {
    event.preventDefault();

    if (this.statusForm().invalid()) {
      this.statusForm().markAsTouched();
      return;
    }

    const name = this.statusForm.name().value().trim();
    const category = this.statusForm.category().value();
    if (!name) return;

    this.dialogRef.close({ name, category });
  }

  categoryLabel(category: StatusCategory) {
    return statusCategoryLabels[category];
  }
}
