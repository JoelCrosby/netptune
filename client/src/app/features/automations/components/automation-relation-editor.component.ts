import { httpResource } from '@angular/common/http';
import { Component, computed, input, output, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RelationType } from '@core/models/relation-type';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { debounceTime } from 'rxjs/operators';
import {
  AutomationAction,
  AutomationRelationDirection,
  AutomationRelationOperation,
} from '../models/automation.models';

interface TaskSearchResponse {
  payload?: {
    items?: TaskViewModel[];
  };
}

@Component({
  selector: 'app-automation-relation-editor',
  imports: [FormInputComponent, FormSelectComponent, FormSelectOptionComponent],
  template: `
    <div class="flex flex-col gap-4">
      <div class="grid gap-3 md:grid-cols-2">
        <app-form-select
          label="Operation"
          [noMargin]="true"
          [value]="operation()"
          (changed)="setOperation($event)">
          <app-form-select-option [value]="relationOperation.add">
            Link tasks
          </app-form-select-option>
          <app-form-select-option [value]="relationOperation.remove">
            Remove links
          </app-form-select-option>
        </app-form-select>

        <app-form-select
          label="Relation"
          [required]="true"
          [noMargin]="true"
          [value]="action().relationTypeId ?? null"
          (changed)="patch.emit({ relationTypeId: $event })">
          <app-form-select-option [value]="null">
            Choose a relation
          </app-form-select-option>
          @for (relationType of relationTypes(); track relationType.id) {
            <app-form-select-option [value]="relationType.id">
              {{ relationType.name }}
            </app-form-select-option>
          }
        </app-form-select>
      </div>

      @if (isAdding()) {
        <app-form-select
          label="Direction"
          [noMargin]="true"
          [value]="direction()"
          (changed)="patch.emit({ relationDirection: $event })">
          <app-form-select-option [value]="relationDirection.taskIsSource">
            The triggering task is the source
          </app-form-select-option>
          <app-form-select-option [value]="relationDirection.taskIsTarget">
            The triggering task is the target
          </app-form-select-option>
        </app-form-select>
      }

      <app-form-input
        label="Find a task"
        placeholder="Search by key or name"
        [noMargin]="true"
        [value]="taskSearch()"
        (valueChange)="taskSearch.set($event)" />

      <app-form-select
        [label]="taskSelectLabel()"
        [noMargin]="true"
        [value]="action().relatedTaskId ?? null"
        (changed)="patch.emit({ relatedTaskId: $event })">
        <app-form-select-option [value]="null">
          {{ emptyTaskOptionLabel() }}
        </app-form-select-option>
        @if (hasUnlistedSelection()) {
          <app-form-select-option [value]="action().relatedTaskId">
            Task #{{ action().relatedTaskId }}
          </app-form-select-option>
        }
        @for (task of tasks(); track task.id) {
          <app-form-select-option [value]="task.id">
            {{ task.systemId }} — {{ task.name }}
          </app-form-select-option>
        }
      </app-form-select>
    </div>
  `,
})
export class AutomationRelationEditorComponent {
  readonly relationOperation = AutomationRelationOperation;
  readonly relationDirection = AutomationRelationDirection;

  readonly action = input.required<AutomationAction>();
  readonly relationTypes = input.required<RelationType[]>();
  readonly patch = output<Partial<AutomationAction>>();

  readonly taskSearch = signal('');

  private readonly search = toSignal(
    toObservable(this.taskSearch).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  private readonly taskResults = httpResource<TaskSearchResponse>(() => {
    const search = this.search().trim();
    const params: Record<string, string> = search ? { search } : {};

    return {
      url: 'api/tasks',
      params,
    };
  });

  readonly tasks = computed(() => {
    return this.taskResults.value()?.payload?.items ?? [];
  });
  readonly operation = computed(() => {
    return this.action().relationOperation ?? AutomationRelationOperation.add;
  });
  readonly direction = computed(() => {
    return (
      this.action().relationDirection ??
      AutomationRelationDirection.taskIsSource
    );
  });
  readonly isAdding = computed(() => {
    return this.operation() === AutomationRelationOperation.add;
  });
  readonly taskSelectLabel = computed(() => {
    return this.isAdding() ? 'Task to link' : 'Limit to one task';
  });
  readonly emptyTaskOptionLabel = computed(() => {
    return this.isAdding() ? 'Choose a task' : 'Every linked task';
  });
  readonly hasUnlistedSelection = computed(() => {
    const relatedTaskId = this.action().relatedTaskId;

    if (!relatedTaskId) return false;

    const isListed = this.tasks().some((task) => task.id === relatedTaskId);

    return !isListed;
  });

  setOperation(operation: AutomationRelationOperation) {
    const isAdding = operation === AutomationRelationOperation.add;

    this.patch.emit({
      relationOperation: operation,
      relationDirection: isAdding
        ? (this.action().relationDirection ??
          AutomationRelationDirection.taskIsSource)
        : null,
    });
  }
}
