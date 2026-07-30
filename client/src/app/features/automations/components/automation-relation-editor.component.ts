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
          i18n-label="Label of the operation field"
          label="Operation"
          [noMargin]="true"
          [value]="operation()"
          (changed)="setOperation($event)">
          <app-form-select-option [value]="relationOperation.add">
            <span i18n="Relation operation that creates a link">
              Link tasks
            </span>
          </app-form-select-option>
          <app-form-select-option [value]="relationOperation.remove">
            <span i18n="Relation operation that removes a link">
              Remove links
            </span>
          </app-form-select-option>
        </app-form-select>

        <app-form-select
          i18n-label="Label of the relation field"
          label="Relation"
          [required]="true"
          [noMargin]="true"
          [value]="action().relationTypeId ?? null"
          (changed)="patch.emit({ relationTypeId: $event })">
          <app-form-select-option [value]="null">
            <span i18n="Placeholder option in the relation picker">
              Choose a relation
            </span>
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
          i18n-label="Label of the direction field"
          label="Direction"
          [noMargin]="true"
          [value]="direction()"
          (changed)="patch.emit({ relationDirection: $event })">
          <app-form-select-option [value]="relationDirection.taskIsSource">
            <span i18n="Relation direction: the triggering task is the source">
              The triggering task is the source
            </span>
          </app-form-select-option>
          <app-form-select-option [value]="relationDirection.taskIsTarget">
            <span i18n="Relation direction: the triggering task is the target">
              The triggering task is the target
            </span>
          </app-form-select-option>
        </app-form-select>
      }

      <app-form-input
        i18n-label="Label of the task search field in the dry-run dialog"
        label="Find a task"
        i18n-placeholder="Placeholder text: Search by key or name"
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
            <span
              i18n="
                Fallback label for a task shown only by id. ID is the task id
              ">
              Task #{{
                action().relatedTaskId // i18n(ph="ID")
              }}
            </span>
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
    return this.isAdding()
      ? $localize`:Label of the field choosing which task to link:Task to link`
      : $localize`:Label of the field narrowing a relation removal to one task:Limit to one task`;
  });
  readonly emptyTaskOptionLabel = computed(() => {
    return this.isAdding()
      ? $localize`:Placeholder in the task picker:Choose a task`
      : $localize`:Option that applies to every linked task:Every linked task`;
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
