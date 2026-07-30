import { Component, forwardRef, input, model, output } from '@angular/core';
import { IconButtonComponent } from '@app/static/components/button/icon-button.component';
import { Status } from '@core/models/status';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { IconCircleComponent } from '@static/components/icon-circle.component';
import { PanelComponent } from '@static/components/panel.component';
import { taskChangeFieldLabels } from '../models/automation-copy';
import {
  AutomationConditionGroup,
  AutomationConditionGroupOperator,
  AutomationConditionOperator,
  AutomationFieldCondition,
  TaskChangeField,
} from '../models/automation.models';
import { AutomationFieldConditionEditorComponent } from './automation-field-condition-editor.component';
import {
  LucideGripVertical,
  LucideLayersPlus,
  LucideListFilter,
  LucideListPlus,
  LucideTrash2,
} from '@lucide/angular';

@Component({
  selector: 'app-automation-condition-group-editor',
  imports: [
    StrokedButtonComponent,
    IconButtonComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    IconCircleComponent,
    PanelComponent,
    AutomationFieldConditionEditorComponent,
    LucideGripVertical,
    LucideLayersPlus,
    LucideListPlus,
    LucideTrash2,
    forwardRef(() => AutomationConditionGroupEditorComponent),
  ],
  template: `
    <app-panel [attr.aria-label]="'Condition group: ' + operatorLabel()">
      <div
        class="border-border bg-foreground/3 flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3">
        <div class="flex flex-wrap items-center gap-3">
          <app-icon-circle [icon]="conditionIcon" />
          <div class="flex flex-wrap items-center gap-2 text-sm">
            <span
              class="text-foreground/60 font-medium"
              i18n="Word before the all/any operator in a condition group">
              Match
            </span>
            <div
              class="border-border bg-background inline-flex rounded-md border p-0.5"
              role="group"
              i18n-aria-label="
                Accessible name of the condition group operator control
              "
              aria-label="Condition group logic">
              @for (option of groupOperatorOptions; track option.value) {
                <button
                  type="button"
                  class="rounded px-2.5 py-1 text-xs font-semibold transition-colors"
                  [class.bg-primary]="group().operator === option.value"
                  [class.text-white]="group().operator === option.value"
                  [class.text-foreground/60]="group().operator !== option.value"
                  [class.hover:bg-foreground/8]="
                    group().operator !== option.value
                  "
                  [attr.aria-pressed]="group().operator === option.value"
                  (click)="setOperator(option.value)">
                  {{ option.label }}
                </button>
              }
            </div>
            <span
              class="text-foreground/60"
              i18n="Word after the all/any operator in a condition group">
              of the following
            </span>
          </div>
        </div>

        @if (clearable()) {
          <button
            type="button"
            class="text-foreground/45 hover:bg-foreground/5 hover:text-foreground/70 focus-visible:ring-primary rounded px-2 py-1.5 text-xs font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none"
            (click)="cleared.emit()">
            <span i18n="Button that removes every condition">
              Clear conditions
            </span>
          </button>
        } @else if (removable()) {
          <button
            app-icon-button
            color="warn"
            type="button"
            i18n-aria-label="
              Accessible label for the button that removes a condition group
            "
            aria-label="Remove condition group"
            i18n-title="Tooltip on the button that removes a condition group"
            title="Remove group"
            (click)="removed.emit()">
            <svg lucideTrash2 class="h-4 w-4"></svg>
          </button>
        }
      </div>

      <div class="flex min-w-0">
        <div
          class="relative hidden w-16 shrink-0 items-center justify-center sm:flex"
          aria-hidden="true">
          <div
            class="bg-primary/30 absolute top-0 bottom-0 left-1/2 w-px"></div>
          <span
            class="bg-primary/10 text-primary relative rounded-full px-2 py-1 text-[0.65rem] font-bold tracking-wider">
            {{ operatorCode() }}
          </span>
        </div>

        <div class="flex min-w-0 flex-1 flex-col gap-3 p-3 sm:pl-0">
          @for (
            condition of group().conditions;
            track $index;
            let conditionIndex = $index
          ) {
            <div
              class="border-border bg-background flex min-w-0 items-start gap-1.5 rounded-md border p-2.5 shadow-xs">
              <div
                class="text-foreground/35 mt-7 hidden h-9 w-5 shrink-0 items-center justify-center sm:flex"
                aria-hidden="true">
                <svg lucideGripVertical class="h-4 w-4"></svg>
              </div>

              <div
                class="grid min-w-0 flex-1 gap-2 lg:grid-cols-[minmax(130px,0.75fr)_minmax(240px,1.5fr)]">
                <app-form-select
                  i18n-label="Label of the field field"
                  label="Field"
                  [noMargin]="true"
                  [value]="condition.field"
                  (valueChange)="setConditionField(conditionIndex, $event)">
                  @for (field of conditionFieldOptions; track field) {
                    <app-form-select-option [value]="field">
                      {{ taskFieldLabel(field) }}
                    </app-form-select-option>
                  }
                </app-form-select>

                <app-automation-field-condition-editor
                  [field]="condition.field"
                  [statuses]="statuses()"
                  [supportsChangeOperators]="supportsChangeOperators()"
                  i18n-operatorLabel="
                    Label of the condition group operator control
                  "
                  operatorLabel="Operator"
                  [condition]="condition"
                  (conditionChange)="setCondition(conditionIndex, $event)" />
              </div>

              <button
                app-icon-button
                class="mt-6 shrink-0"
                color="warn"
                type="button"
                i18n-aria-label="
                  Accessible label for the button that removes a condition
                "
                aria-label="Remove condition"
                i18n-title="Tooltip on the button that removes a condition"
                title="Remove condition"
                (click)="removeCondition(conditionIndex)">
                <svg lucideTrash2 class="h-4 w-4"></svg>
              </button>
            </div>
          }

          @for (
            nestedGroup of group().groups;
            track $index;
            let groupIndex = $index
          ) {
            <div class="border-primary/25 min-w-0 border-l-2 pl-3">
              <app-automation-condition-group-editor
                [group]="nestedGroup"
                [statuses]="statuses()"
                [supportsChangeOperators]="supportsChangeOperators()"
                [depth]="depth() + 1"
                [removable]="true"
                (groupChange)="setGroup(groupIndex, $event)"
                (removed)="removeGroup(groupIndex)" />
            </div>
          }

          @if (!group().conditions.length && !group().groups.length) {
            <div
              class="border-border text-foreground/55 rounded-md border border-dashed px-4 py-6 text-center text-sm">
              <span i18n="Empty state for a condition group">
                This group is empty. Add a condition or a nested group.
              </span>
            </div>
          }

          <div class="flex flex-wrap items-center gap-1 pt-1">
            <button
              app-stroked-button
              class="gap-2"
              type="button"
              (click)="addCondition()">
              <svg lucideListPlus class="h-4 w-4"></svg>
              <span i18n="Button that adds a condition">Add condition</span>
            </button>

            <button
              app-stroked-button
              class="gap-2"
              type="button"
              [disabled]="depth() >= maximumDepth"
              [title]="
                depth() >= maximumDepth
                  ? 'Maximum nesting depth reached'
                  : 'Add a nested condition group'
              "
              (click)="addGroup()">
              <svg lucideLayersPlus class="h-4 w-4"></svg>
              <span i18n="Button that adds a nested condition group">
                Add group
              </span>
            </button>
          </div>
        </div>
      </div>
    </app-panel>
  `,
})
export class AutomationConditionGroupEditorComponent {
  conditionIcon = LucideListFilter;
  maximumDepth = 4;
  automationConditionGroupOperator = AutomationConditionGroupOperator;

  conditionFieldOptions = [
    TaskChangeField.name,
    TaskChangeField.description,
    TaskChangeField.status,
    TaskChangeField.assignees,
    TaskChangeField.owner,
    TaskChangeField.priority,
    TaskChangeField.estimate,
    TaskChangeField.dueDate,
    TaskChangeField.tags,
    TaskChangeField.startDate,
  ];
  groupOperatorOptions = [
    {
      label: $localize`:Condition group operator requiring every condition to match:All`,
      value: AutomationConditionGroupOperator.all,
    },
    {
      label: $localize`:Condition group operator requiring at least one condition to match:Any`,
      value: AutomationConditionGroupOperator.any,
    },
    {
      label: $localize`:Condition group operator requiring no condition to match:None`,
      value: AutomationConditionGroupOperator.none,
    },
  ];

  group = model.required<AutomationConditionGroup>();
  statuses = input.required<Status[]>();
  supportsChangeOperators = input(false);
  depth = input(1);
  removable = input(false);
  clearable = input(false);
  removed = output();
  cleared = output();

  taskFieldLabel(field: TaskChangeField): string {
    return taskChangeFieldLabels[field];
  }

  operatorLabel(): string {
    return (
      this.groupOperatorOptions.find(
        (option) => option.value === this.group().operator
      )?.label ?? 'All'
    );
  }

  operatorCode(): string {
    switch (this.group().operator) {
      case AutomationConditionGroupOperator.any:
        return 'OR';
      case AutomationConditionGroupOperator.none:
        return 'NOT';
      default:
        return 'AND';
    }
  }

  setOperator(operator: AutomationConditionGroupOperator | null) {
    if (operator === null) return;

    this.group.update((group) => ({ ...group, operator }));
  }

  addCondition() {
    const condition: AutomationFieldCondition = {
      field: TaskChangeField.status,
      operator: AutomationConditionOperator.equals,
      value: null,
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

  setConditionField(index: number, field: TaskChangeField | null) {
    if (field === null) return;

    this.setCondition(index, {
      field,
      operator: AutomationConditionOperator.any,
      value: null,
    });
  }

  setCondition(index: number, condition: AutomationFieldCondition) {
    this.group.update((group) => ({
      ...group,
      conditions: group.conditions.map((item, itemIndex) =>
        itemIndex === index ? condition : item
      ),
    }));
  }

  addGroup() {
    if (this.depth() >= this.maximumDepth) return;

    const nestedGroup: AutomationConditionGroup = {
      operator: AutomationConditionGroupOperator.all,
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

  setGroup(index: number, nestedGroup: AutomationConditionGroup) {
    this.group.update((group) => ({
      ...group,
      groups: group.groups.map((item, itemIndex) =>
        itemIndex === index ? nestedGroup : item
      ),
    }));
  }
}
