import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import {
  apply,
  FormField,
  form,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-edit-sprint-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormInputComponent,
    FormTextAreaComponent,
    FormField,
  ],
  template: `
    <app-dialog-title i18n="Title of the edit-sprint dialog">
      Edit Sprint
    </app-dialog-title>

    <form class="flex flex-col gap-3" (submit)="onSubmit($event)">
      <app-form-input
        i18n-label="Label of the name field"
        label="Name"
        maxLength="256"
        [formField]="sprintForm.name" />

      <app-form-textarea
        i18n-label="Label of the sprint goal field"
        label="Goal"
        rows="3"
        maxLength="32768"
        [formField]="sprintForm.goal" />

      <div class="grid grid-cols-2 gap-3">
        <app-form-input
          i18n-label="Label of the sprint start date field"
          label="Start"
          type="date"
          [formField]="sprintForm.startDate" />

        <app-form-input
          i18n-label="Label of the sprint end date field"
          label="End"
          type="date"
          [formField]="sprintForm.endDate" />
      </div>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="dialogRef.close()">
        <span i18n="Dismisses a dialog without acting">Cancel</span>
      </button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="updateLoading()"
        (click)="onSubmit($event)">
        <span i18n="Confirms and stores an edit">Save</span>
      </button>
    </div>
  `,
})
export class EditSprintDialogComponent {
  dialogRef = inject<DialogRef<EditSprintDialogComponent>>(DialogRef);
  sprint = inject<SprintViewModel>(DIALOG_DATA);

  private readonly sprintCommands = inject(SprintCommandsService);

  readonly updateLoading = this.sprintCommands.isUpdating;

  readonly sprintFormModel = signal({
    name: this.sprint.name,
    goal: this.sprint.goal ?? '',
    startDate: toDateInputValue(new Date(this.sprint.startDate)),
    endDate: toDateInputValue(new Date(this.sprint.endDate)),
  });

  readonly sprintForm = form(this.sprintFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Name is required.":Name`,
        maxLength: 256,
      })
    );
    maxLength(schema.goal, 32768);
    required(schema.startDate, {
      message: $localize`:Validation error when the sprint start date is empty:Start date is required.`,
    });
    required(schema.endDate, {
      message: $localize`:Validation error when the sprint end date is empty:End date is required.`,
    });
    validate(schema.endDate, (context) => {
      const startDate = context.valueOf(schema.startDate);
      const endDate = context.value();

      if (!startDate || !endDate || endDate >= startDate) return undefined;

      return {
        kind: 'dateOrder',
        message: $localize`:Validation error when a sprint ends before it starts:End date must be on or after the start date.`,
      };
    });
  });

  onSubmit(event: Event) {
    event.preventDefault();

    if (!this.sprint.id) return;

    submit(this.sprintForm, async () => {
      const value = this.sprintFormModel();

      this.sprintCommands.update({
        id: this.sprint.id,
        name: value.name.trim(),
        goal: value.goal.trim() || null,
        startDate: value.startDate,
        endDate: value.endDate,
      });

      this.dialogRef.close();
    });
  }
}

function toDateInputValue(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}
