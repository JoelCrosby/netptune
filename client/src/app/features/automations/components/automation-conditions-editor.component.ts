import { Component, computed, input, model } from '@angular/core';
import { Status } from '@core/models/status';
import {
  emptyQueryBuilderGroup,
  newQueryCondition,
  QueryBuilderGroup,
} from '@shared/components/query-builder/query-builder.models';
import { QueryChipBarComponent } from '@shared/components/query-builder/query-chip-bar.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  automationConditionCatalog,
  fromBuilderGroup,
  toBuilderGroup,
} from '../models/automation-query-builder';
import { AutomationConditionGroup } from '../models/automation.models';
import { AutomationFlowCardComponent } from './automation-flow-card.component';

@Component({
  selector: 'app-automation-conditions-editor',
  imports: [
    AutomationFlowCardComponent,
    QueryChipBarComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-automation-flow-card
      i18n-keyword="Heading of the conditions part of the rule"
      keyword="IF"
      i18n-heading="Heading above the conditions"
      heading="Conditions">
      <span flowCardActions class="text-foreground/45 text-xs">
        <span i18n="Marks the conditions section as not required"
          >optional</span
        >
      </span>

      @if (conditionGroup()) {
        <button
          flowCardActions
          type="button"
          class="text-foreground/45 hover:bg-foreground/5 hover:text-foreground/70 focus-visible:ring-primary rounded px-2 py-1.5 text-xs font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none"
          (click)="conditionGroup.set(null)">
          <span i18n="Button that removes every condition">
            Clear conditions
          </span>
        </button>
      }

      @if (conditionGroup()) {
        <app-query-chip-bar
          [group]="builderGroup()"
          [catalog]="catalog()"
          i18n-summaryPrefix="
            Prefix of the plain-language summary of an automation's conditions
          "
          summaryPrefix="Continues only when"
          [emptySummary]="emptySummary"
          (groupChange)="setGroup($event)" />
      } @else {
        <p
          class="border-border bg-foreground/2 text-foreground/60 rounded-lg border border-dashed px-3 py-2.5 text-[13px]">
          <span i18n="Shown when a rule has no conditions">
            No conditions — every task that fires the trigger continues.
          </span>
        </p>

        <button
          app-stroked-button
          class="self-start"
          type="button"
          (click)="addConditionGroup()">
          <span i18n="Button that starts adding conditions">Add condition</span>
        </button>
      }
    </app-automation-flow-card>
  `,
})
export class AutomationConditionsEditorComponent {
  readonly statuses = input.required<Status[]>();
  readonly supportsChangeOperators = input(false);
  readonly conditionGroup = model<AutomationConditionGroup | null>(null);

  readonly emptySummary = $localize`:Shown when an automation rule has conditions but none are filled in yet:No conditions yet, so every matching task continues.`;

  readonly catalog = computed(() => {
    return automationConditionCatalog(
      this.statuses(),
      this.supportsChangeOperators()
    );
  });

  readonly builderGroup = computed(() => {
    const group = this.conditionGroup();

    return group ? toBuilderGroup(group) : emptyQueryBuilderGroup();
  });

  setGroup(group: QueryBuilderGroup) {
    this.conditionGroup.set(fromBuilderGroup(group));
  }

  // Conditions start with one row rather than an empty group, because the point of pressing
  // "Add condition" is to write one.
  addConditionGroup() {
    const group = emptyQueryBuilderGroup();
    const field = this.catalog().fields[0];

    this.setGroup(
      field ? { ...group, conditions: [newQueryCondition(field)] } : group
    );
  }
}
