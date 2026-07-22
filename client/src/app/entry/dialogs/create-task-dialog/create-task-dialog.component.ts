import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import {
  FormField,
  form,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
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
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
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
    FormErrorsComponent,
    TaskPropertiesComponent,
  ],
  template: `<app-dialog-title>
      <div class="px-6">Create Task</div>
    </app-dialog-title>

    <form
      id="create-task-form"
      app-dialog-content
      novalidate
      (submit)="saveClicked($event)">
      <div class="flex flex-col gap-8 px-6 md:flex-row md:gap-12">
        <div class="flex w-92 grow flex-col">
          <app-form-input
            [formField]="taskForm.name"
            label="Summary"
            maxLength="256" />

          <label
            id="description-label"
            class="font-sm mb-2 font-semibold"
            for="description">
            Description
          </label>
          <app-editor
            id="description"
            aria-labelledby="description-label"
            placeholder="Add a Description..."
            [formField]="taskForm.description"
            [isReadonly]="false" />
          <app-form-errors [formField]="taskForm.description" />
        </div>

        <div
          class="bg-card/40 flex w-full shrink-0 flex-col rounded px-6 pb-6 md:w-72">
          <app-task-properties
            [(statusId)]="statusId"
            [(priority)]="priority"
            [(projectId)]="projectId"
            [(sprintId)]="sprintId"
            [(startDate)]="startDate"
            [(dueDate)]="dueDate"
            [(assignees)]="assignees"
            [estimateType]="estimateType()"
            [estimateValue]="estimateValue()"
            [showProject]="!data?.projectId"
            [showSprint]="!data?.sprintId"
            [multiple]="false"
            (estimateChange)="setEstimate($event)" />

          @if (scheduleInvalid()) {
            <p class="mt-3 text-sm text-red-600" role="alert">
              Start date must be on or before due date.
            </p>
          }

          @if (projectInvalid()) {
            <p class="mt-3 text-sm text-red-600" role="alert">
              Project is required.
            </p>
          }
        </div>
      </div>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">Close</button>
      <button app-flat-button type="submit" form="create-task-form">
        Save Task
      </button>
    </div> `,
})
export class CreateTaskDialogComponent {
  static readonly width = '972px';

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
  readonly startDate = signal('');
  readonly dueDate = signal('');
  readonly projectId = signal<number | null>(
    this.data?.projectId ?? this.currentProjectId() ?? null
  );
  readonly assignees = signal<(AppUser | AssigneeViewModel)[]>([]);
  readonly submissionAttempted = signal(false);
  readonly scheduleInvalid = computed(() => {
    const startDate = this.startDate();
    const dueDate = this.dueDate();

    return startDate !== '' && dueDate !== '' && startDate > dueDate;
  });
  readonly projectInvalid = computed(
    () => this.submissionAttempted() && this.projectId() === null
  );

  taskFormModel = signal<CreateTaskForm>({
    name: '',
    description: '',
  });

  taskForm = form(this.taskFormModel, (schema) => {
    required(schema.name, { message: 'Summary is required.' });
    validate(schema.name, ({ value }) => {
      const valueToValidate = value();

      if (!valueToValidate) return undefined;

      const name = valueToValidate.trim();

      if (!name) {
        return { kind: 'whitespace', message: 'Summary is required.' };
      }

      if (name.length < 4) {
        return {
          kind: 'minLength',
          message: 'Summary must have at least 4 characters.',
        };
      }

      if (name.length > 256) {
        return {
          kind: 'maxLength',
          message: 'Summary cannot exceed 256 characters.',
        };
      }

      return undefined;
    });
    maxLength(schema.description, 4096, {
      message: 'Description cannot exceed 4096 characters.',
    });
  });

  setEstimate({ estimateType, estimateValue }: TaskEstimate) {
    this.estimateType.set(estimateType);
    this.estimateValue.set(estimateValue);
  }

  close() {
    this.dialogRef.close();
  }

  saveClicked(event: Event) {
    event.preventDefault();
    this.submissionAttempted.set(true);

    submit(this.taskForm, async () => {
      if (this.scheduleInvalid()) return;

      const workspace = this.currentWorkspace();
      const projectId = this.projectId();

      if (projectId === null) return;

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
    });
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
      startDate: this.startDate() || null,
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
