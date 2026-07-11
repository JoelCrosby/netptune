import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { FormField, form, minLength, required } from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { loadProjects } from '@core/store/projects/projects.actions';
import {
  selectAllProjects,
  selectCurrentProjectId,
} from '@core/store/projects/projects.selectors';
import { createProjectTask } from '@core/store/tasks/tasks.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { EditorComponent } from '@app/static/components/editor/editor.component';

// When opened from a sprint, the project is fixed to the sprint's project and
// the created task is attached to the sprint, so the project selector is hidden.
export interface CreateTaskDialogData {
  projectId?: number;
  sprintId?: number;
}

@Component({
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    EditorComponent,
  ],
  template: `<app-dialog-title>Add new Task</app-dialog-title>

    <form app-dialog-content novalidate>
      <app-form-input
        [formField]="taskForm.name"
        label="Summary"
        maxLength="1024" />

      <app-editor
        [formField]="taskForm.description"
        label="Description"
        maxLength="4096"
        [isReadonly]="false" />

      @if (!data?.projectId) {
        <app-form-select [formField]="taskForm.project" label="Project">
          @for (project of projects(); track project.id) {
            <app-form-select-option [value]="project.id">
              {{ project.name }}
            </app-form-select-option>
          }
        </app-form-select>
      }
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button (click)="close()">Close</button>
      <button app-flat-button (click)="saveClicked()">Save Task</button>
    </div> `,
})
export class CreateTaskDialogComponent {
  private store = inject(Store);
  dialogRef = inject<DialogRef<CreateTaskDialogComponent>>(DialogRef);
  readonly data = inject<CreateTaskDialogData | null>(DIALOG_DATA, {
    optional: true,
  });

  projects = this.store.selectSignal(selectAllProjects);
  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);
  projectId = this.store.selectSignal(selectCurrentProjectId);

  selectedTypeValue!: number;

  taskFormModel = signal({
    name: '',
    project: this.data?.projectId ?? this.projectId() ?? '',
    description: '',
  });

  taskForm = form(this.taskFormModel, (schema) => {
    required(schema.name);
    minLength(schema.name, 4);
  });

  constructor() {
    this.store.dispatch(loadProjects.init());
  }

  close() {
    this.dialogRef.close();
  }

  saveClicked() {
    const workspace = this.currentWorkspace();
    const projectId = this.data?.projectId ?? this.projectId();

    if (projectId === undefined || projectId === null) {
      throw new Error('project id is undefined');
    }

    const { name, description } = this.taskForm;

    const task: AddProjectTaskRequest = {
      name: name().value().trim(),
      description: description().value().trim(),
      projectId,
      sprintId: this.data?.sprintId ?? null,
    };

    if (!workspace?.slug) {
      throw new Error('workspace slug is undefined');
    }

    this.store.dispatch(
      createProjectTask.init({
        identifier: `[workspace] ${workspace.slug}`,
        task,
      })
    );

    this.dialogRef.close();
  }
}
