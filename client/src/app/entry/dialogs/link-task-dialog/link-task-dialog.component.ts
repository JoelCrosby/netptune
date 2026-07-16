import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Params } from '@angular/router';
import { FormField, form, required } from '@angular/forms/signals';
import { RelationType, isSymmetricCategory } from '@core/models/relation-type';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { relationTypeResource } from '@core/resources/relation-type.resources';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { debounceTime } from 'rxjs/operators';

export interface LinkTaskDialogData {
  task: TaskViewModel;
}

export interface LinkTaskDialogResult {
  relationTypeId: number;
  isForward: boolean;
  tasks: readonly TaskViewModel[];
}

@Component({
  selector: 'app-link-task-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormField,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
    TaskScopeIdComponent,
  ],
  template: `
    <app-dialog-title>Link Tasks</app-dialog-title>

    <div class="flex w-220 max-w-full flex-col gap-4">
      <div class="flex gap-4">
        <app-form-select
          class="flex-1"
          [formField]="linkForm.relationTypeId"
          label="Relation">
          @for (relationType of relationTypes(); track relationType.id) {
            <app-form-select-option [value]="relationType.id">
              {{ relationType.name }}
            </app-form-select-option>
          }
        </app-form-select>

        @if (!isSymmetric()) {
          <app-form-select
            class="flex-1"
            [formField]="linkForm.isForward"
            label="Direction">
            <app-form-select-option [value]="true">
              {{ forwardLabel() }}
            </app-form-select-option>
            <app-form-select-option [value]="false">
              {{ inverseLabel() }}
            </app-form-select-option>
          </app-form-select>
        }
      </div>

      <p class="text-muted-foreground text-sm">
        {{ summary() }}
      </p>

      <app-form-input
        name="link-task-search"
        placeholder="Search tasks by name, key or tag"
        [noMargin]="true"
        [value]="searchInput()"
        (valueChange)="searchInput.set($event)" />

      <app-datatable
        containerClass="h-[380px] overflow-y-auto overflow-x-hidden"
        tableClass="table-fixed"
        rowClass="bg-card"
        emptyMessage="No tasks available to link."
        [data]="data"
        [selection]="true"
        [stickyHeader]="true"
        (selectionChanged)="selected.set($event)">
        <ng-template appDatatableCell="systemId" let-task>
          <app-task-scope-id [id]="task.systemId" />
        </ng-template>

        <ng-template appDatatableCell="name" let-task>
          <span class="block truncate font-medium">{{ task.name }}</span>
        </ng-template>

        <ng-template appDatatableCell="status" let-task>
          <span class="text-muted-foreground text-xs">
            {{ task.statusName }}
          </span>
        </ng-template>
      </app-datatable>
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">Cancel</button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="selected().length === 0 || !selectedRelationType()"
        (click)="submit()">
        Link
        {{ selected().length }}
        {{ selected().length === 1 ? 'task' : 'tasks' }}
      </button>
    </div>
  `,
})
export class LinkTaskDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<LinkTaskDialogResult, LinkTaskDialogComponent>>(DialogRef);
  private readonly dialogData = inject<LinkTaskDialogData>(DIALOG_DATA);

  private readonly relationTypesResource = relationTypeResource();
  readonly relationTypes = computed(() =>
    [...this.relationTypesResource.value()].sort(
      (a, b) => a.sortOrder - b.sortOrder || a.id - b.id
    )
  );

  readonly searchInput = signal('');
  readonly selected = signal<readonly TaskViewModel[]>([]);

  readonly linkFormModel = signal({
    relationTypeId: 0,
    isForward: true,
  });

  readonly linkForm = form(this.linkFormModel, (schema) => {
    required(schema.relationTypeId);
  });

  readonly selectedRelationType = computed<RelationType | undefined>(() => {
    const id = this.linkForm.relationTypeId().value();
    const relationTypes = this.relationTypes();

    // The select starts empty, so fall back to the first type once they load.
    return relationTypes.find((type) => type.id === id) ?? relationTypes[0];
  });

  readonly isSymmetric = computed(() => {
    const relationType = this.selectedRelationType();

    return relationType ? isSymmetricCategory(relationType.category) : false;
  });

  readonly forwardLabel = computed(
    () => `This task ${this.selectedRelationType()?.name ?? ''} the selected`
  );

  readonly inverseLabel = computed(
    () =>
      `This task ${this.selectedRelationType()?.inverseName ?? ''} the selected`
  );

  readonly summary = computed(() => {
    const relationType = this.selectedRelationType();

    if (!relationType) return '';

    const label = this.isForward()
      ? relationType.name
      : relationType.inverseName;

    return `${this.dialogData.task.systemId} ${label} the tasks you select below.`;
  });

  // Debounce so each keystroke doesn't trigger a server fetch.
  private search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  // excludeTaskId keeps the current task from being offered as something to link to itself.
  private params = computed<Params>(() => {
    const search = this.search().trim();

    return {
      excludeTaskId: this.dialogData.task.id,
      ...(search ? { search } : {}),
    };
  });

  readonly data: DatatableDataSource<TaskViewModel> = {
    key: 'link-tasks',
    columns: [
      { id: 'systemId', header: 'Key', sortable: true, widthClass: 'w-28' },
      {
        id: 'name',
        header: 'Task',
        accessor: 'name',
        sortable: true,
        cellClass: 'min-w-0',
      },
      { id: 'status', header: 'Status', sortable: true, widthClass: 'w-40' },
    ],
    resource: {
      url: 'api/tasks',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
  };

  private isForward() {
    return this.isSymmetric() ? true : this.linkForm.isForward().value();
  }

  submit() {
    const relationType = this.selectedRelationType();
    const tasks = this.selected();

    if (!relationType || tasks.length === 0) return;

    this.dialogRef.close({
      relationTypeId: relationType.id,
      isForward: this.isForward(),
      tasks,
    });
  }

  close() {
    this.dialogRef.close();
  }
}
