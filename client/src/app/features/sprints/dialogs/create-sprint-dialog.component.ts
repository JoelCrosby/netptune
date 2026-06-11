import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, effect, inject } from '@angular/core';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { createSprint } from '@core/store/sprints/sprints.actions';
import { selectSprintCreateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';

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
  ],
  template: `
    <app-dialog-title>Create Sprint</app-dialog-title>

    <form class="flex flex-col gap-3" (ngSubmit)="onSubmit()">
      <app-form-select
        label="Project"
        placeholder="Select project"
        [value]="projectId ?? null"
        (changed)="onProjectSelected($event)">
        @for (project of projects(); track project.id) {
          <app-form-select-option [value]="project.id!">
            {{ project.name }}
          </app-form-select-option>
        }
      </app-form-select>

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
        [disabled]="createLoading() || !projectId || !name.trim()"
        (click)="onSubmit()">
        Create
      </button>
    </div>
  `,
})
export class CreateSprintDialogComponent {
  private store = inject(Store);
  dialogRef = inject<DialogRef<CreateSprintDialogComponent>>(DialogRef);

  readonly createLoading = this.store.selectSignal(selectSprintCreateLoading);
  readonly projects = this.store.selectSignal(selectAllProjects);

  readonly defaultDates = computed(() => {
    const start = new Date();
    const end = new Date();
    end.setDate(start.getDate() + 14);
    return {
      start: toDateInputValue(start),
      end: toDateInputValue(end),
    };
  });

  projectId?: number;
  name = '';
  goal = '';
  startDate = this.defaultDates().start;
  endDate = this.defaultDates().end;
  dateError?: string;

  constructor() {
    effect(() => {
      const firstProject = this.projects()[0];
      if (!this.projectId && firstProject) {
        this.projectId = firstProject.id;
      }
    });
  }

  onProjectSelected(projectId: number) {
    this.projectId = projectId;
  }

  onSubmit() {
    if (!this.projectId || !this.name.trim()) return;

    if (this.endDate < this.startDate) {
      this.dateError = 'End date must be after start date.';
      return;
    }

    this.dateError = undefined;

    this.store.dispatch(
      createSprint({
        request: {
          projectId: this.projectId,
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
