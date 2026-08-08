import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, effect, inject, signal } from '@angular/core';
import {
  apply,
  FormField,
  form,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { projectResource } from '@core/resources/project.resource';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-create-sprint-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormTextAreaComponent,
    FormField,
  ],
  template: `
    <app-dialog-title i18n="Title of the create-sprint dialog">
      Create Sprint
    </app-dialog-title>

    <form class="flex flex-col gap-3" (submit)="onSubmit($event)">
      <app-form-select
        i18n-label="Label of the project field"
        label="Project"
        i18n-placeholder="Placeholder in the project picker"
        placeholder="Select project"
        [formField]="sprintForm.projectId">
        @for (project of projects(); track project.id) {
          <app-form-select-option [value]="project.id!">
            {{ project.name }}
          </app-form-select-option>
        }
      </app-form-select>

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
        [disabled]="createLoading()"
        (click)="onSubmit($event)">
        <span i18n="Button that creates the sprint">Create</span>
      </button>
    </div>
  `,
})
export class CreateSprintDialogComponent {
  dialogRef = inject<DialogRef<CreateSprintDialogComponent>>(DialogRef);

  private readonly sprintCommands = inject(SprintCommandsService);

  readonly createLoading = this.sprintCommands.isCreating;
  readonly projectsResource = projectResource();
  readonly projects = this.projectsResource.value;

  readonly defaultDates = computed(() => {
    const start = new Date();
    const end = new Date();
    end.setDate(start.getDate() + 14);
    return {
      start: toDateInputValue(start),
      end: toDateInputValue(end),
    };
  });

  readonly sprintFormModel = signal({
    projectId: null as number | null,
    name: '',
    goal: '',
    startDate: this.defaultDates().start,
    endDate: this.defaultDates().end,
  });

  readonly sprintForm = form(this.sprintFormModel, (schema) => {
    required(schema.projectId, {
      message: $localize`:Validation error when no project is selected:Project is required.`,
    });
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

  constructor() {
    effect(() => {
      const firstProject = this.projects()[0];
      if (!this.sprintForm.projectId().value() && firstProject) {
        this.sprintForm.projectId().value.set(firstProject.id);
      }
    });
  }

  onSubmit(event: Event) {
    event.preventDefault();

    submit(this.sprintForm, async () => {
      const value = this.sprintFormModel();

      if (!value.projectId) return;

      this.sprintCommands.create({
        projectId: value.projectId,
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
