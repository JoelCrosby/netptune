import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { FormField, form, minLength, required } from '@angular/forms/signals';
import { EditorComponent } from '@app/static/components/editor/editor.component';
import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import { AppUser } from '@core/models/appuser';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { selectCurrentProjectId } from '@core/store/projects/projects.selectors';
import { createProjectTask } from '@core/store/tasks/tasks.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { TaskEstimate } from '@static/components/task-properties/task-estimate-select.component';
import { TaskPropertiesComponent } from '@static/components/task-properties/task-properties.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

export interface CreateTaskDialogData {
  projectId?: number;
  sprintId?: number;
}

interface CreateTaskForm {
  name: string;
  description: string;
}

@Component({
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    EditorComponent,
    TaskPropertiesComponent,
  ],
  template: `<app-dialog-title>Add new Task</app-dialog-title>

    <form app-dialog-content novalidate (submit)="$event.preventDefault()">
      <div class="flex flex-col gap-8 md:flex-row md:gap-12">
        <div class="flex w-64 grow flex-col">
          <app-form-input
            [formField]="taskForm.name"
            label="Summary"
            maxLength="1024" />

          <label class="font-sm mb-2 font-semibold" for="description">
            Description
          </label>
          <app-editor
            aria-labelledby="description"
            placeholder="Add a Description..."
            [formField]="taskForm.description"
            maxLength="4096"
            [isReadonly]="false" />
        </div>

        <div
          class="bg-card/40 flex w-full shrink-0 flex-col rounded px-6 pb-6 md:w-72">
          <app-task-properties
            [(statusId)]="statusId"
            [(priority)]="priority"
            [(projectId)]="projectId"
            [(sprintId)]="sprintId"
            [(dueDate)]="dueDate"
            [(assignees)]="assignees"
            [estimateType]="estimateType()"
            [estimateValue]="estimateValue()"
            [showProject]="!data?.projectId"
            [showSprint]="!data?.sprintId"
            [multiple]="false"
            (estimateChange)="setEstimate($event)" />
        </div>
      </div>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button (click)="close()">Close</button>
      <button app-flat-button (click)="saveClicked()">Save Task</button>
    </div> `,
})
export class CreateTaskDialogComponent {
  static readonly width = '820px';

  private store = inject(Store);
  dialogRef = inject<DialogRef<CreateTaskDialogComponent>>(DialogRef);
  readonly data = inject<CreateTaskDialogData | null>(DIALOG_DATA, {
    optional: true,
  });

  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);
  currentProjectId = this.store.selectSignal(selectCurrentProjectId);

  readonly statusId = signal<number | null>(null);
  readonly priority = signal<TaskPriority | null>(null);
  readonly estimateType = signal<EstimateType | null>(null);
  readonly estimateValue = signal<number | null>(null);
  readonly sprintId = signal<number | null>(this.data?.sprintId ?? null);
  readonly dueDate = signal('');
  readonly projectId = signal<number | null>(
    this.data?.projectId ?? this.currentProjectId() ?? null
  );
  readonly assignees = signal<(AppUser | AssigneeViewModel)[]>([]);

  taskFormModel = signal<CreateTaskForm>({
    name: '',
    description: '',
  });

  taskForm = form(this.taskFormModel, (schema) => {
    required(schema.name);
    minLength(schema.name, 4);
  });

  setEstimate({ estimateType, estimateValue }: TaskEstimate) {
    this.estimateType.set(estimateType);
    this.estimateValue.set(estimateValue);
  }

  close() {
    this.dialogRef.close();
  }

  saveClicked() {
    const workspace = this.currentWorkspace();
    const projectId = this.projectId();

    if (projectId === null) {
      throw new Error('project id is undefined');
    }

    if (!workspace?.slug) {
      throw new Error('workspace slug is undefined');
    }

    this.store.dispatch(
      createProjectTask.init({
        identifier: `[workspace] ${workspace.slug}`,
        task: this.buildRequest(projectId),
      })
    );

    this.dialogRef.close();
  }

  private buildRequest(projectId: number): AddProjectTaskRequest {
    const { name, description } = this.taskFormModel();
    const assigneeId = this.assignees().at(0)?.id;
    const estimateType = this.estimateType();
    const estimateValue = this.estimateValue();
    const priority = this.priority();
    const statusId = this.statusId();

    const task: AddProjectTaskRequest = {
      name: name.trim(),
      description: description.trim(),
      projectId,
      sprintId: this.sprintId(),
      dueDate: this.dueDate() || null,
    };

    if (statusId !== null) task.statusId = statusId;
    if (priority !== null) task.priority = priority;
    if (assigneeId) task.assigneeId = assigneeId;

    if (estimateType !== null) {
      task.estimateType = estimateType;
      if (estimateValue !== null) task.estimateValue = estimateValue;
    }

    return task;
  }
}
