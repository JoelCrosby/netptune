import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { Field, form, minLength, required } from '@angular/forms/signals';
import { MatButton } from '@angular/material/button';
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
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

@Component({
  templateUrl: './create-task-dialog.component.html',
  styleUrls: ['./create-task-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Field,
    FormInputComponent,
    FormTextAreaComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    DialogActionsDirective,
    MatButton,
  ],
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
