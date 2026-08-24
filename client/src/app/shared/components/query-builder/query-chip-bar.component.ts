import { Component, computed, forwardRef, input, model } from '@angular/core';
import { LucideLayersPlus, LucidePlus, LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import {
  emptyQueryBuilderGroup,
  newQueryCondition,
  QueryBuilderCatalog,
  QueryBuilderCondition,
  QueryBuilderError,
  QueryBuilderGroup,
  QueryBuilderGroupOperator,
  queryBuilderGroupOperatorCodes,
  queryBuilderGroupOperatorLabels,
} from './query-builder.models';
import { explainQueryGroup } from './query-explanation';
import { QueryChipComponent } from './query-chip.component';
import { QueryStatusComponent } from './query-status.component';

type GroupOperatorValue = 'all' | 'any' | 'none';

const operatorsByValue: Record<GroupOperatorValue, QueryBuilderGroupOperator> =
  {
    all: QueryBuilderGroupOperator.all,
    any: QueryBuilderGroupOperator.any,
    none: QueryBuilderGroupOperator.none,
  };

const valuesByOperator: Record<QueryBuilderGroupOperator, GroupOperatorValue> =
  {
    [QueryBuilderGroupOperator.all]: 'all',
    [QueryBuilderGroupOperator.any]: 'any',
    [QueryBuilderGroupOperator.none]: 'none',
  };

/**
 * Flat query builder: the whole query reads as one line of tokens. Conditions are chips, nested
 * groups are indented chip clusters, and every editor lives in a popover so the query never pushes
 * what it filters off screen. The catalog decides which fields and operators exist, so the same
 * component edits a saved view's query and an automation rule's conditions.
 */
@Component({
  selector: 'app-query-chip-bar',
  imports: [
    IconButtonComponent,
    SegmentedControlComponent,
    StrokedButtonComponent,
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
        app-stroked-button
        color="neutral"
        [class]="addButtonClass"
        type="button"
        [disabled]="atConditionLimit()"
        (click)="addCondition()">
        <svg lucidePlus class="h-3.5 w-3.5"></svg>
        <span i18n="Button that adds a query condition">Condition</span>
      </button>

      <button
        app-stroked-button
        color="neutral"
        [class]="addButtonClass"
        type="button"
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
          app-icon-button
          color="warn"
          class="h-7 w-7 shrink-0 rounded-md"
          type="button"
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
        [prefix]="statusPrefix()"
        [summary]="summary()" />
    }
  `,
})
export class QueryChipBarComponent {
  readonly operatorOptions: SegmentedOption<GroupOperatorValue>[] = [
    {
      value: 'all',
      label: queryBuilderGroupOperatorLabels[QueryBuilderGroupOperator.all],
    },
    {
      value: 'any',
      label: queryBuilderGroupOperatorLabels[QueryBuilderGroupOperator.any],
    },
    {
      value: 'none',
      label: queryBuilderGroupOperatorLabels[QueryBuilderGroupOperator.none],
    },
  ];

  // Dashed outline marks these as "add something", which no button variant covers.
  readonly addButtonClass = 'h-9 gap-1.5 rounded-[9px] border-dashed px-3';

  readonly group = model.required<QueryBuilderGroup>();
  readonly catalog = input.required<QueryBuilderCatalog>();
  readonly errors = input<QueryBuilderError[]>([]);
  readonly path = input('query');
  readonly depth = input(1);
  readonly nested = input(false);
  // What the summary line reads as. Both belong to the caller, because only it knows what the
  // query selects: tasks a view lists, or tasks an automation is allowed to act on.
  readonly summaryPrefix = input('');
  readonly emptySummary = input('');

  readonly operatorValue = computed(
    () => valuesByOperator[this.group().operator]
  );

  readonly joiner = computed(
    () => queryBuilderGroupOperatorCodes[this.group().operator]
  );

  private readonly explanation = computed(() => {
    return explainQueryGroup(this.group(), this.catalog());
  });

  readonly summary = computed(() => this.explanation() || this.emptySummary());

  readonly statusPrefix = computed(() => {
    return this.explanation() ? this.summaryPrefix() : '';
  });

  readonly messages = computed(() => {
    return this.errors().map((error) => error.message);
  });

  atDepthLimit(): boolean {
    return this.depth() >= this.catalog().maximumDepth;
  }

  atConditionLimit(): boolean {
    const limit = this.catalog().maximumConditionCount;

    if (limit === undefined) return false;

    return this.group().conditions.length >= limit;
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

    this.group.update((group) => ({
      ...group,
      conditions: [...group.conditions, newQueryCondition(field)],
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

  setCondition(index: number, condition: QueryBuilderCondition) {
    this.group.update((group) => ({
      ...group,
      conditions: group.conditions.map((item, itemIndex) => {
        return itemIndex === index ? condition : item;
      }),
    }));
  }

  addGroup() {
    if (this.atDepthLimit()) return;

    const nestedGroup = emptyQueryBuilderGroup(QueryBuilderGroupOperator.any);

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

  setGroup(index: number, nestedGroup: QueryBuilderGroup) {
    this.group.update((group) => ({
      ...group,
      groups: group.groups.map((item, itemIndex) => {
        return itemIndex === index ? nestedGroup : item;
      }),
    }));
  }
}
