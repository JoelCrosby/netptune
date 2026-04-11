import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormField, form, minLength, required } from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { TaskStatus } from '@core/enums/project-task-status';
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
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    FormTextAreaComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Add new Task</app-dialog-title>

    <form app-dialog-content>
      <app-form-input
        [formField]="taskForm.name"
        label="Summary"
        maxLength="1024">
      </app-form-input>

      <app-form-textarea
        [formField]="taskForm.description"
        label="Description"
        maxLength="4096">
      </app-form-textarea>

      <app-form-select [formField]="taskForm.project" label="Project">
        @for (project of projects(); track project.id) {
          <app-form-select-option [value]="project.id">
            {{ project.name }}
          </app-form-select-option>
        }
      </app-form-select>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button (click)="close()">Close</button>
      <button app-flat-button (click)="saveClicked()">Save Task</button>
    </div> `,
})
export class CreateTaskDialogComponent {
  private store = inject(Store);
  dialogRef = inject<DialogRef<CreateTaskDialogComponent>>(DialogRef);

  projects = this.store.selectSignal(selectAllProjects);
  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);
  projectId = this.store.selectSignal(selectCurrentProjectId);

  selectedTypeValue!: number;

  taskFormModel = signal({
    name: '',
    project: this.projectId() ?? '',
    description: '',
  });

  taskForm = form(this.taskFormModel, (schema) => {
    required(schema.name);
    minLength(schema.name, 4);
  });

  constructor() {
    this.store.dispatch(loadProjects());
  }

  close() {
    this.dialogRef.close();
  }

  saveClicked() {
    const workspace = this.currentWorkspace();
    const projectId = this.projectId();

    if (projectId === undefined || projectId === null) {
      throw new Error('project id is undefined');
    }

    const { name, description } = this.taskForm;

    const task: AddProjectTaskRequest = {
      name: name().value().trim(),
      description: description().value().trim(),
      projectId,
      status: TaskStatus.new,
    };

    if (!workspace?.slug) {
      throw new Error('workspace slug is undefined');
    }

    this.store.dispatch(
      createProjectTask({
        identifier: `[workspace] ${workspace.slug}`,
        task,
      })
    );

    this.dialogRef.close();
  }
}
