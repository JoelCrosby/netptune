import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { Store } from '@ngrx/store';
import { BulkUpdateTasksRequest } from '@core/models/requests/bulk-update-tasks-request';
import { bulkUpdateTasks } from '@core/store/tasks/tasks.actions';
import { WorkspaceAppUser } from '@core/models/appuser';
import { ClientResponse } from '@core/models/client-response';
import { MAX_PAGE_SIZE, Page } from '@core/models/pagination';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { SprintStatus } from '@core/enums/sprint-status';
import { estimateTypeOptions } from '@core/enums/estimate-type';
import { taskPriorityOptions } from '@core/enums/task-priority';
import { statusResource } from '@core/resources/status.resources';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { selectCurrentWorkspaceIdentifier } from '@app/core/store/workspaces/workspaces.selectors';

const NO_SPRINT = -1;

interface BulkEditTasksForm {
  statusId: number | null;
  priority: number | null;
  estimateType: number | null;
  estimateValue: string;
  projectId: number | null;
  sprintId: number | null;
  assigneeIds: string[];
}

@Component({
  selector: 'app-bulk-edit-tasks-dialog',
  imports: [
    FormField,
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormSelectTagsComponent,
    FormSelectTagsOptionComponent,
    FormInputComponent,
  ],
  template: `
    <app-dialog-title>Bulk edit tasks</app-dialog-title>

    <form app-dialog-content>
      <p class="text-muted mb-4 text-sm">
        Applying to {{ taskCount() }}
        {{ taskCount() === 1 ? 'task' : 'tasks' }}. Fields left on
        <span class="font-medium">Keep current</span> are not changed.
      </p>

      <div class="grid grid-cols-1 gap-x-4 sm:grid-cols-2">
        <app-form-select [formField]="editForm.statusId" label="Status">
          <app-form-select-option [value]="null">
            Keep current
          </app-form-select-option>
          @for (status of statuses.value(); track status.id) {
            <app-form-select-option [value]="status.id">
              {{ status.name }}
            </app-form-select-option>
          }
        </app-form-select>

        <app-form-select [formField]="editForm.priority" label="Priority">
          <app-form-select-option [value]="null">
            Keep current
          </app-form-select-option>
          @for (option of priorityOptions; track option.value) {
            <app-form-select-option [value]="option.value">
              {{ option.label }}
            </app-form-select-option>
          }
        </app-form-select>

        <app-form-select [formField]="editForm.projectId" label="Project">
          <app-form-select-option [value]="null">
            Keep current
          </app-form-select-option>
          @for (project of projects.value(); track project.id) {
            <app-form-select-option [value]="project.id">
              {{ project.name }}
            </app-form-select-option>
          }
        </app-form-select>

        <app-form-select [formField]="editForm.sprintId" label="Sprint">
          <app-form-select-option [value]="null">
            Keep current
          </app-form-select-option>
          <app-form-select-option [value]="noSprint">
            No sprint
          </app-form-select-option>
          @for (sprint of sprints.value(); track sprint.id) {
            <app-form-select-option [value]="sprint.id">
              {{ sprint.name }}
            </app-form-select-option>
          }
        </app-form-select>

        <app-form-select
          [formField]="editForm.estimateType"
          label="Estimate type">
          <app-form-select-option [value]="null">
            Keep current
          </app-form-select-option>
          @for (option of estimateOptions; track option.value) {
            <app-form-select-option [value]="option.value">
              {{ option.label }}
            </app-form-select-option>
          }
        </app-form-select>

        <app-form-input
          [formField]="editForm.estimateValue"
          type="number"
          label="Story points" />
      </div>

      <app-form-select-tags
        [formField]="editForm.assigneeIds"
        label="Assignees">
        @for (user of assignableUsers(); track user.id) {
          <app-form-select-tags-option [value]="user.id">
            {{ user.displayName }}
          </app-form-select-tags-option>
        }
      </app-form-select-tags>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button (click)="close()">Cancel</button>
      <button app-flat-button (click)="apply()">
        Apply to {{ taskCount() }} {{ taskCount() === 1 ? 'task' : 'tasks' }}
      </button>
    </div>
  `,
})
export class BulkEditTasksDialogComponent {
  static readonly width = '640px';

  private readonly dialogRef =
    inject<DialogRef<void, BulkEditTasksDialogComponent>>(DialogRef);
  private readonly store = inject(Store);
  readonly tasks = inject<number[]>(DIALOG_DATA);

  readonly noSprint = NO_SPRINT;
  readonly priorityOptions = taskPriorityOptions;
  readonly estimateOptions = estimateTypeOptions;
  readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  readonly taskCount = computed(() => this.tasks.length);

  readonly statuses = statusResource();

  readonly projects = httpResource<ProjectViewModel[]>(
    () => ({
      url: 'api/projects',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    }),
    { defaultValue: [] }
  );

  readonly sprints = httpResource<SprintViewModel[]>(
    () => ({
      url: 'api/sprints',
      params: {
        statuses: [SprintStatus.planning, SprintStatus.active],
        take: 100,
      },
    }),
    { defaultValue: [] }
  );

  private readonly users = httpResource<ClientResponse<Page<WorkspaceAppUser>>>(
    () => ({ url: 'api/users', params: { page: 1, pageSize: MAX_PAGE_SIZE } })
  );

  readonly assignableUsers = computed(() =>
    (this.users.value()?.payload?.items ?? []).filter((user) => !user.isPending)
  );

  readonly editFormModel = signal<BulkEditTasksForm>({
    statusId: null,
    priority: null,
    estimateType: null,
    estimateValue: '',
    projectId: null,
    sprintId: null,
    assigneeIds: [],
  });

  readonly editForm = form(this.editFormModel);

  close() {
    this.dialogRef.close();
  }

  apply() {
    const workspaceId = this.workspaceId();

    if (!workspaceId) return;

    this.store.dispatch(
      bulkUpdateTasks.init({
        identifier: `[workspace] ${workspaceId}`,
        request: this.buildRequest(),
      })
    );

    this.dialogRef.close();
  }

  private buildRequest(): BulkUpdateTasksRequest {
    const value = this.editFormModel();

    const request: BulkUpdateTasksRequest = {
      taskIds: this.tasks,
    };

    if (value.statusId !== null) request.statusId = value.statusId;
    if (value.priority !== null) request.priority = value.priority;
    if (value.estimateType !== null) request.estimateType = value.estimateType;
    if (value.projectId !== null) request.projectId = value.projectId;

    const estimateValue = value.estimateValue.trim();

    if (estimateValue !== '') {
      const parsed = Number(estimateValue);

      if (!Number.isNaN(parsed)) request.estimateValue = parsed;
    }

    if (value.sprintId === NO_SPRINT) {
      request.clearSprint = true;
    } else if (value.sprintId !== null) {
      request.sprintId = value.sprintId;
    }

    if (value.assigneeIds.length > 0) request.assigneeIds = value.assigneeIds;

    return request;
  }
}
