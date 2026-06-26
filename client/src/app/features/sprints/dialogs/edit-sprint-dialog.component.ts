import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject } from '@angular/core';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { updateSprint } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';

@Component({
  selector: 'app-edit-sprint-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormInputComponent,
    FormTextAreaComponent,
  ],
  template: `
    <app-dialog-title>Edit Sprint</app-dialog-title>

    <form class="flex flex-col gap-3" (ngSubmit)="onSubmit()">
      <app-form-input
        label="Name"
        name="name"
        [required]="true"
        [(value)]="name" />

      <app-form-textarea label="Goal" name="goal" rows="3" [(value)]="goal" />

      <div class="grid grid-cols-2 gap-3">
        <app-form-input
          label="Start"
          name="startDate"
          type="date"
          [required]="true"
          [(value)]="startDate" />

        <app-form-input
          label="End"
          name="endDate"
          type="date"
          [required]="true"
          [(value)]="endDate" />
      </div>

      @if (dateError) {
        <p class="text-sm text-red-600">{{ dateError }}</p>
      }
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="dialogRef.close()">
        Cancel
      </button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="updateLoading() || !name.trim()"
        (click)="onSubmit()">
        Save
      </button>
    </div>
  `,
})
export class EditSprintDialogComponent {
  private store = inject(Store);
  dialogRef = inject<DialogRef<EditSprintDialogComponent>>(DialogRef);
  sprint = inject<SprintViewModel>(DIALOG_DATA);

  readonly updateLoading = this.store.selectSignal(selectSprintUpdateLoading);

  name = this.sprint.name;
  goal = this.sprint.goal ?? '';
  startDate = toDateInputValue(new Date(this.sprint.startDate));
  endDate = toDateInputValue(new Date(this.sprint.endDate));
  dateError?: string;

  onSubmit() {
    if (!this.sprint.id || !this.name.trim()) return;

    if (this.endDate < this.startDate) {
      this.dateError = 'End date must be after start date.';
      return;
    }

    this.dateError = undefined;

    this.store.dispatch(
      updateSprint.init({
        request: {
          id: this.sprint.id,
          name: this.name.trim(),
          goal: this.goal.trim() || null,
          startDate: this.startDate,
          endDate: this.endDate,
        },
      })
    );

    this.dialogRef.close();
  }
}

function toDateInputValue(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}
