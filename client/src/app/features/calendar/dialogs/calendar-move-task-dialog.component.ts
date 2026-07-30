import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { FormField, form, required, submit } from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { CalendarTaskMoveRequest } from '../models/calendar.models';

export interface CalendarMoveTaskDialogResult {
  date: string;
}

@Component({
  selector: 'app-calendar-move-task-dialog',
  imports: [
    DialogActionsDirective,
    DialogCloseDirective,
    DialogTitleComponent,
    FlatButtonComponent,
    FormField,
    FormInputComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title
      i18n="
        Title of the dialog for moving a task to another date. TASK_ID is the
        task key
      ">
      Move
      {{
        data.task.systemId // i18n(ph="TASK_ID")
      }}
    </app-dialog-title>

    <form (submit)="submitMove($event)">
      <p class="text-muted mb-5 max-w-96 text-sm">
        <span
          i18n="
            Instructions in the move-task dialog. TASK_NAME is the task name
          ">
          Choose where to move
          {{
            data.task.name  // i18n(ph="TASK_NAME")
          }}. Its duration will be preserved.
        </span>
      </p>

      <app-form-input
        [formField]="moveForm.date"
        i18n-label="Label of the destination date field"
        label="Move to"
        type="date"
        [noMargin]="true" />

      <div app-dialog-actions align="end">
        <button app-stroked-button app-dialog-close type="button">
          <span i18n="Dismisses a dialog without acting">Cancel</span>
        </button>
        <button
          app-flat-button
          color="primary"
          type="submit"
          [disabled]="moveForm().invalid()">
          <span i18n="Button that moves the task to the chosen date">Move</span>
        </button>
      </div>
    </form>
  `,
})
export class CalendarMoveTaskDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<CalendarMoveTaskDialogResult, CalendarMoveTaskDialogComponent>
    >(DialogRef);

  readonly data = inject<CalendarTaskMoveRequest>(DIALOG_DATA);
  readonly moveFormModel = signal({ date: this.data.fromDate });
  readonly moveForm = form(this.moveFormModel, (schema) => {
    required(schema.date);
  });

  protected submitMove(event: Event): void {
    event.preventDefault();

    submit(this.moveForm, async () => {
      this.dialogRef.close({ date: this.moveForm.date().value() });
    });
  }
}
