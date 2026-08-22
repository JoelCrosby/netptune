import {
  Component,
  computed,
  forwardRef,
  inject,
  input,
  model,
} from '@angular/core';
import {
  taskQueryGroupOperatorCodes,
  taskQueryGroupOperatorLabels,
} from '@app/features/task-views/models/task-query-copy';
import {
  TaskQueryCatalog,
  TaskQueryCondition,
  TaskQueryGroup,
  TaskQueryGroupOperator,
  TaskQueryOperator,
  TaskQueryValidationError,
} from '@app/features/task-views/models/task-view.models';
import { QueryFieldOptionsService } from '@app/features/task-views/services/query-field-options.service';
import { explainQuery } from '@app/features/task-views/util/query-explanation';
import { LucideLayersPlus, LucidePlus, LucideX } from '@lucide/angular';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { QueryChipComponent } from './query-chip.component';
import { QueryStatusComponent } from './query-status.component';

type GroupOperatorValue = 'all' | 'any' | 'none';

const operatorsByValue: Record<GroupOperatorValue, TaskQueryGroupOperator> = {
  all: TaskQueryGroupOperator.all,
  any: TaskQueryGroupOperator.any,
  none: TaskQueryGroupOperator.none,
};

const valuesByOperator: Record<TaskQueryGroupOperator, GroupOperatorValue> = {
  [TaskQueryGroupOperator.all]: 'all',
  [TaskQueryGroupOperator.any]: 'any',
  [TaskQueryGroupOperator.none]: 'none',
};

/**
 * Flat replacement for the stacked query builder: the whole query reads as one line of tokens.
 * Conditions are chips, nested groups are indented chip clusters, and every editor lives in a
 * popover so the query never pushes the preview off screen.
 */
@Component({
  selector: 'app-query-chip-bar',
  imports: [
    SegmentedControlComponent,
    QueryChipComponent,
    QueryStatusComponent,
    LucidePlus,
    LucideLayersPlus,
    LucideX,
    forwardRef(() => QueryChipBarComponent),
  ],
  host: { class: 'block' },
  template: `
    <div class="flex flex-wrap items-center gap-2.5">
      @if (!nested()) {
        <div class="flex shrink-0 items-center gap-2">
          <span
            class="text-foreground/45 text-[13px]"
            i18n="Prefix of the query group logic control">
            Match
          </span>

          <app-segmented-control
            [options]="operatorOptions"
            i18n-ariaLabel="Accessible name of the query group operator control"
            ariaLabel="Query group logic"
            [value]="operatorValue()"
            (valueChange)="setOperatorValue($event)" />
        </div>

        <div class="bg-foreground/10 h-5.5 w-px shrink-0"></div>
      }

      @for (condition of group().conditions; track $index; let index = $index) {
        <div class="flex items-center gap-2">
          @if (index > 0) {
            <span
              class="text-primary/75 text-[11px] font-bold tracking-[0.09em]">
              {{ joiner() }}
            </span>
          }

          <app-query-chip
            [catalog]="catalog()"
            [condition]="condition"
            [invalid]="!!errorFor(index)"
            (conditionChange)="setCondition(index, $event)"
            (removed)="removeCondition(index)" />
        </div>
      }

      <button
        type="button"
        class="text-foreground/60 hover:border-primary/70 hover:text-primary flex h-9 items-center gap-1.5 rounded-[9px] border border-dashed border-white/20 px-3 text-sm transition-colors disabled:pointer-events-none disabled:opacity-50"
        [disabled]="atConditionLimit()"
        (click)="addCondition()">
        <svg lucidePlus class="h-3.5 w-3.5"></svg>
        <span i18n="Button that adds a query condition">Condition</span>
      </button>

      <button
        type="button"
        class="text-foreground/60 hover:border-primary/70 hover:text-primary flex h-9 items-center gap-1.5 rounded-[9px] border border-dashed border-white/20 px-3 text-sm transition-colors disabled:pointer-events-none disabled:opacity-50"
        [disabled]="atDepthLimit()"
        (click)="addGroup()">
        <svg lucideLayersPlus class="h-3.5 w-3.5"></svg>
        <span i18n="Button that adds a nested query group">Group</span>
      </button>
    </div>

    @for (
      nestedGroup of group().groups;
      track $index;
      let groupIndex = $index
    ) {
      <div
        class="border-primary/40 bg-foreground/2 mt-2.5 flex items-start gap-2.5 rounded-r-[9px] border-l-2 py-2 pr-2 pl-3">
        <span
          class="text-primary/75 mt-2 shrink-0 text-[11px] font-bold tracking-[0.09em]">
          {{ joiner() }}
        </span>

        <app-query-chip-bar
          class="min-w-0 flex-1"
          [nested]="true"
          [group]="nestedGroup"
          [catalog]="catalog()"
          [errors]="errors()"
          [path]="path() + '.groups[' + groupIndex + ']'"
          [depth]="depth() + 1"
          (groupChange)="setGroup(groupIndex, $event)" />

        <button
          type="button"
          class="text-foreground/35 hover:text-warn hover:bg-warn/10 flex h-7 w-7 shrink-0 items-center justify-center rounded-md transition-colors"
          i18n-aria-label="
            Accessible label for the button that removes a query group
          "
          aria-label="Remove query group"
          (click)="removeGroup(groupIndex)">
          <svg lucideX class="h-3.5 w-3.5"></svg>
        </button>
      </div>
    }

    @if (!nested()) {
      <app-query-status
        class="mt-3"
        [messages]="messages()"
        [summary]="summary()" />
    }
  `,
})
export class QueryChipBarComponent {
  private readonly fieldOptions = inject(QueryFieldOptionsService);

  readonly operatorOptions: SegmentedOption<GroupOperatorValue>[] = [
    {
      value: 'all',
      label: taskQueryGroupOperatorLabels[TaskQueryGroupOperator.all],
    },
    {
      value: 'any',
      label: taskQueryGroupOperatorLabels[TaskQueryGroupOperator.any],
    },
    {
      value: 'none',
      label: taskQueryGroupOperatorLabels[TaskQueryGroupOperator.none],
    },
  ];

  readonly group = model.required<TaskQueryGroup>();
  readonly catalog = input.required<TaskQueryCatalog>();
  readonly errors = input<TaskQueryValidationError[]>([]);
  readonly path = input('query');
  readonly depth = input(1);
  readonly nested = input(false);

  readonly operatorValue = computed(
    () => valuesByOperator[this.group().operator]
  );

  readonly joiner = computed(
    () => taskQueryGroupOperatorCodes[this.group().operator]
  );

  readonly summary = computed(() => {
    const catalog = this.catalog();

    return explainQuery(this.group(), {
      catalog,
      labelFor: (fieldKey, value) => {
        const field = catalog.fields.find(
          (candidate) => candidate.key === fieldKey
        );

        return this.fieldOptions.labelFor(field, value);
      },
    });
  });

  readonly messages = computed(() => {
    return this.errors().map((error) => error.message);
  });

  atDepthLimit(): boolean {
    return this.depth() >= this.catalog().maximumDepth;
  }

  atConditionLimit(): boolean {
    return (
      this.group().conditions.length >= this.catalog().maximumConditionCount
    );
  }

  errorFor(conditionIndex: number): string | null {
    const path = `${this.path()}.conditions[${conditionIndex}]`;
    const match = this.errors().find((error) => error.path === path);

    return match?.message ?? null;
  }

  setOperatorValue(value: GroupOperatorValue) {
    this.group.update((group) => ({
      ...group,
      operator: operatorsByValue[value],
    }));
  }

  addCondition() {
    const field = this.catalog().fields[0];

    if (!field) return;

    const condition: TaskQueryCondition = {
      field: field.key,
      operator: field.operators[0] ?? TaskQueryOperator.equals,
      values: [],
    };

    this.group.update((group) => ({
      ...group,
      conditions: [...group.conditions, condition],
    }));
  }

  removeCondition(index: number) {
    this.group.update((group) => ({
      ...group,
      conditions: group.conditions.filter(
        (_, itemIndex) => itemIndex !== index
      ),
    }));
  }

  setCondition(index: number, condition: TaskQueryCondition) {
    this.group.update((group) => ({
      ...group,
      conditions: group.conditions.map((item, itemIndex) => {
        return itemIndex === index ? condition : item;
      }),
    }));
  }

  addGroup() {
    if (this.atDepthLimit()) return;

    const nestedGroup: TaskQueryGroup = {
      operator: TaskQueryGroupOperator.any,
      conditions: [],
      groups: [],
    };

    this.group.update((group) => ({
      ...group,
      groups: [...group.groups, nestedGroup],
    }));
  }

  removeGroup(index: number) {
    this.group.update((group) => ({
      ...group,
      groups: group.groups.filter((_, itemIndex) => itemIndex !== index),
    }));
  }

  setGroup(index: number, nestedGroup: TaskQueryGroup) {
    this.group.update((group) => ({
      ...group,
      groups: group.groups.map((item, itemIndex) => {
        return itemIndex === index ? nestedGroup : item;
      }),
    }));
  }
}
