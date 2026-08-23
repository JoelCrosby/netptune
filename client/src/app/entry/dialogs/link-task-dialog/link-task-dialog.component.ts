import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Params } from '@angular/router';
import { FormField, form, required } from '@angular/forms/signals';
import { RelationType, isSymmetricCategory } from '@core/models/relation-type';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { relationTypeResource } from '@core/resources/relation-type.resource';
import { taskColumns } from '@core/tasks/task-columns';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { TaskTableComponent } from '@static/components/task-table.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { debounceTime } from 'rxjs/operators';

export interface LinkTaskDialogData {
  // Absent while linking from the create-task dialog, where the task does not exist yet.
  task?: TaskViewModel;
}

export interface LinkTaskDialogResult {
  relationTypeId: number;
  relationType: RelationType;
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
    TaskTableComponent,
  ],
  template: `
    <app-dialog-title i18n="Title of the dialog for linking tasks together">
      Link Tasks
    </app-dialog-title>

    <div class="flex w-220 max-w-full flex-col gap-4">
      <div class="flex gap-4">
        <app-form-select
          class="flex-1"
          [formField]="linkForm.relationTypeId"
          i18n-label="Label of the relation type field"
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
            i18n-label="Label of the field choosing which way a relation points"
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

      <p class="text-muted text-sm">
        {{ summary() }}
      </p>

      <app-form-input
        name="link-task-search"
        i18n-placeholder="Placeholder in the box for finding tasks to link"
        placeholder="Search tasks by name, key or tag"
        [noMargin]="true"
        [value]="searchInput()"
        (valueChange)="searchInput.set($event)" />

      <app-task-table
        containerClass="h-[380px] overflow-y-auto overflow-x-hidden"
        key="link-tasks"
        url="api/tasks"
        tableClass="table-fixed"
        i18n-emptyMessage="Shown when no tasks match the link search"
        emptyMessage="No tasks available to link."
        [columns]="columns"
        [params]="params"
        [selection]="true"
        [stickyHeader]="true"
        (selectionChanged)="selected.set($event)" />
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">
        <span i18n="Dismisses a dialog without acting">Cancel</span>
      </button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="selected().length === 0 || !selectedRelationType()"
        (click)="submit()">
        <span i18n="Button that creates the task link">Link</span>
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
    const subject = this.dialogData.task?.systemId ?? 'This task';

    return `${subject} ${label} the tasks you select below.`;
  });

  // Debounce so each keystroke doesn't trigger a server fetch.
  private search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  // excludeTaskId keeps the current task from being offered as something to link to itself.
  readonly params = computed<Params>(() => {
    const search = this.search().trim();
    const excludeTaskId = this.dialogData.task?.id;

    return {
      ...(excludeTaskId ? { excludeTaskId } : {}),
      ...(search ? { search } : {}),
    };
  });

  readonly columns = taskColumns<TaskViewModel>(
    ['systemId', 'name', 'status'],
    { overrides: { name: { cellClass: 'min-w-0' } } }
  );

  private isForward() {
    return this.isSymmetric() ? true : this.linkForm.isForward().value();
  }

  submit() {
    const relationType = this.selectedRelationType();
    const tasks = this.selected();

    if (!relationType || tasks.length === 0) return;

    this.dialogRef.close({
      relationTypeId: relationType.id,
      relationType,
      isForward: this.isForward(),
      tasks,
    });
  }

  close() {
    this.dialogRef.close();
  }
}
