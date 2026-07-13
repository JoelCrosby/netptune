import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { FormField, form, required } from '@angular/forms/signals';
import {
  RelationCategory,
  isSymmetricCategory,
  relationCategoryDescriptions,
  relationCategoryLabels,
  relationCategoryOptions,
} from '@core/models/relation-type';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

export interface CreateRelationTypeDialogResult {
  name: string;
  inverseName: string;
  category: RelationCategory;
}

@Component({
  selector: 'app-create-relation-type-dialog',
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
  template: `<app-dialog-title>Create Relation Type</app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <app-form-select [formField]="relationTypeForm.category" label="Category">
        @for (category of categories; track category) {
          <app-form-select-option [value]="category">
            {{ categoryLabel(category) }}
          </app-form-select-option>
        }
      </app-form-select>

      <p class="text-muted-foreground mb-4 text-sm">
        {{ categoryDescription() }} The category cannot be changed later.
      </p>

      <app-form-input
        [formField]="relationTypeForm.name"
        label="Name"
        placeholder="Blocks"
        maxLength="128" />

      @if (!isSymmetric()) {
        <app-form-input
          [formField]="relationTypeForm.inverseName"
          label="Inverse name"
          placeholder="Is Blocked By"
          maxLength="128" />
      }
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">Close</button>
      <button app-flat-button type="button" (click)="submit($event)">
        Create Relation Type
      </button>
    </div>`,
})
export class CreateRelationTypeDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<
        CreateRelationTypeDialogResult,
        CreateRelationTypeDialogComponent
      >
    >(DialogRef);

  readonly categories = relationCategoryOptions;

  readonly relationTypeFormModel = signal({
    name: '',
    inverseName: '',
    category: RelationCategory.dependency,
  });

  readonly relationTypeForm = form(this.relationTypeFormModel, (schema) => {
    required(schema.name);
    required(schema.category);
  });

  readonly isSymmetric = computed(() =>
    isSymmetricCategory(this.relationTypeForm.category().value())
  );

  readonly categoryDescription = computed(
    () => relationCategoryDescriptions[this.relationTypeForm.category().value()]
  );

  submit(event: Event) {
    event.preventDefault();

    if (this.relationTypeForm().invalid()) {
      this.relationTypeForm().markAsTouched();
      return;
    }

    const name = this.relationTypeForm.name().value().trim();
    const category = this.relationTypeForm.category().value();
    if (!name) return;

    // A symmetric type reads the same both ways, so the inverse mirrors the name. The server
    // enforces this too — it is not relying on the client to have got it right.
    const inverseName = this.isSymmetric()
      ? name
      : this.relationTypeForm.inverseName().value().trim() || name;

    this.dialogRef.close({ name, inverseName, category });
  }

  categoryLabel(category: RelationCategory) {
    return relationCategoryLabels[category];
  }
}
