import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import {
  apply,
  FormField,
  form,
  maxLength,
  required,
  submit,
} from '@angular/forms/signals';
import {
  Status,
  StatusCategory,
  statusCategoryLabels,
  statusCategoryOptions,
} from '@core/models/status';
import { fallbackColor } from '@core/util/colors/colors';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

export interface EditStatusDialogResult {
  name: string;
  color: string;
  category: StatusCategory;
}

@Component({
  selector: 'app-edit-status-dialog',
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    ColorSelectComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Edit Status</app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <app-form-input
        [formField]="statusForm.name"
        label="Name"
        maxLength="128" />

      <app-color-select [formField]="statusForm.color" label="Color" />

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
        Save Status
      </button>
    </div>`,
})
export class EditStatusDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<EditStatusDialogResult, EditStatusDialogComponent>>(
      DialogRef
    );

  readonly data = inject<Status>(DIALOG_DATA);
  readonly categories = statusCategoryOptions;

  readonly statusFormModel = signal({
    name: this.data.name,
    color: this.data.color ?? fallbackColor,
    category: this.data.category,
  });

  readonly statusForm = form(this.statusFormModel, (schema) => {
    apply(schema.name, requiredTextSchema({ label: 'Name', maxLength: 128 }));
    maxLength(schema.color, 32);
    required(schema.color);
    required(schema.category);
  });

  submit(event: Event) {
    event.preventDefault();

    submit(this.statusForm, async () => {
      const name = this.statusForm.name().value().trim();
      const color = this.statusForm.color().value();
      const category = this.statusForm.category().value();

      this.dialogRef.close({ name, color, category });
    });
  }

  categoryLabel(category: StatusCategory) {
    return statusCategoryLabels[category];
  }
}
