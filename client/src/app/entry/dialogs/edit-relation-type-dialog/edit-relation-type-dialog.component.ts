import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { FormField, form, required } from '@angular/forms/signals';
import {
  RelationType,
  isSymmetricCategory,
  relationCategoryLabels,
} from '@core/models/relation-type';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

export interface EditRelationTypeDialogResult {
  name: string;
  inverseName: string;
  color: string;
}

@Component({
  selector: 'app-edit-relation-type-dialog',
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    ColorSelectComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Edit Relation Type</app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <p class="text-muted-foreground mb-4 text-sm">
        Category: {{ categoryLabel }}. A relation type's category is fixed once
        it exists, because changing it would hold existing links to rules they
        were never checked against.
      </p>

      <app-form-input
        [formField]="relationTypeForm.name"
        label="Name"
        maxLength="128" />

      @if (!isSymmetric) {
        <app-form-input
          [formField]="relationTypeForm.inverseName"
          label="Inverse name"
          maxLength="128" />
      }

      <app-color-select [formField]="relationTypeForm.color" label="Color" />
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">Close</button>
      <button app-flat-button type="button" (click)="submit($event)">
        Save Relation Type
      </button>
    </div>`,
})
export class EditRelationTypeDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<EditRelationTypeDialogResult, EditRelationTypeDialogComponent>
    >(DialogRef);

  readonly data = inject<RelationType>(DIALOG_DATA);

  readonly isSymmetric = isSymmetricCategory(this.data.category);
  readonly categoryLabel = relationCategoryLabels[this.data.category];

  readonly relationTypeFormModel = signal({
    name: this.data.name,
    inverseName: this.data.inverseName,
    color: this.data.color ?? '#64748b',
  });

  readonly relationTypeForm = form(this.relationTypeFormModel, (schema) => {
    required(schema.name);
    required(schema.color);
  });

  submit(event: Event) {
    event.preventDefault();

    if (this.relationTypeForm().invalid()) {
      this.relationTypeForm().markAsTouched();
      return;
    }

    const name = this.relationTypeForm.name().value().trim();
    const color = this.relationTypeForm.color().value();
    if (!name) return;

    const inverseName = this.isSymmetric
      ? name
      : this.relationTypeForm.inverseName().value().trim() || name;

    this.dialogRef.close({ name, inverseName, color });
  }
}
