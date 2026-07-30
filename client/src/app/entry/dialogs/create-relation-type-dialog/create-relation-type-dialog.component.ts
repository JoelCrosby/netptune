import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import {
  apply,
  FormField,
  form,
  maxLength,
  required,
  submit,
} from '@angular/forms/signals';
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
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

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
  template: `
    <app-dialog-title i18n="Title of the create-relation-type dialog">
      Create Relation Type
    </app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <app-form-select
        [formField]="relationTypeForm.category"
        i18n-label="Label of the relation category field"
        label="Category">
        @for (category of categories; track category) {
          <app-form-select-option [value]="category">
            {{ categoryLabel(category) }}
          </app-form-select-option>
        }
      </app-form-select>

      <p class="text-muted mb-4 text-sm">
        <span
          i18n="
            Explains the selected relation category and that it is permanent.
            CATEGORY_DESCRIPTION is a sentence describing the chosen category
          ">
          {{
            categoryDescription() // i18n(ph="CATEGORY_DESCRIPTION")
          }}
          The category cannot be changed later.
        </span>
      </p>

      <app-form-input
        [formField]="relationTypeForm.name"
        i18n-label="Label of the name field"
        label="Name"
        i18n-placeholder="Example relation type name shown as placeholder text"
        placeholder="Blocks"
        maxLength="128" />

      @if (!isSymmetric()) {
        <app-form-input
          [formField]="relationTypeForm.inverseName"
          i18n-label="
            Label of the field for the reverse direction of a relation
          "
          label="Inverse name"
          i18n-placeholder="
            Example inverse relation type name shown as placeholder text
          "
          placeholder="Is Blocked By"
          maxLength="128" />
      }
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without saving">Close</span>
      </button>
      <button app-flat-button type="button" (click)="submit($event)">
        <span i18n="Button that creates the relation type">
          Create Relation Type
        </span>
      </button>
    </div>
  `,
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
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Name is required.":Name`,
        maxLength: 128,
      })
    );
    maxLength(schema.inverseName, 128);
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

    submit(this.relationTypeForm, async () => {
      const name = this.relationTypeForm.name().value().trim();
      const category = this.relationTypeForm.category().value();

      // A symmetric type reads the same both ways, so the inverse mirrors the name. The server
      // enforces this too — it is not relying on the client to have got it right.
      const inverseName = this.isSymmetric()
        ? name
        : this.relationTypeForm.inverseName().value().trim() || name;

      this.dialogRef.close({ name, inverseName, category });
    });
  }

  categoryLabel(category: RelationCategory) {
    return relationCategoryLabels[category];
  }
}
