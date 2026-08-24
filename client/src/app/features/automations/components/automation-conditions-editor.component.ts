import { Component, computed, input, model } from '@angular/core';
import { Status } from '@core/models/status';
import {
  emptyQueryBuilderGroup,
  newQueryCondition,
  QueryBuilderGroup,
} from '@shared/components/query-builder/query-builder.models';
import { QueryChipBarComponent } from '@shared/components/query-builder/query-chip-bar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  automationConditionCatalog,
  fromBuilderGroup,
  toBuilderGroup,
} from '../models/automation-query-builder';
import { AutomationConditionGroup } from '../models/automation.models';

@Component({
  selector: 'app-automation-conditions-editor',
  imports: [BadgeComponent, QueryChipBarComponent, StrokedButtonComponent],
  template: `
    <div class="flex flex-col gap-4">
      <div>
        <div class="flex flex-wrap items-center justify-between gap-2">
          <div class="flex items-center gap-2">
            <h2 class="font-overpass text-xl font-medium">
              <span
                i18n="
                  Heading of the conditions section — the 'if' part of the rule
                ">
                If
              </span>
            </h2>
            <app-badge class="text-[0.65rem] tracking-wide uppercase">
              <span i18n="Marks the conditions section as not required">
                Optional
              </span>
            </app-badge>
          </div>

          @if (conditionGroup()) {
            <button
              type="button"
              class="text-foreground/45 hover:bg-foreground/5 hover:text-foreground/70 focus-visible:ring-primary rounded px-2 py-1.5 text-xs font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none"
              (click)="conditionGroup.set(null)">
              <span i18n="Button that removes every condition">
                Clear conditions
              </span>
            </button>
          }
        </div>
        <p class="text-foreground/60 text-sm">
          <span i18n="Explains what conditions do">
            Restrict which tasks can continue to the follow-up actions.
          </span>
        </p>
      </div>

      @if (conditionGroup()) {
        <div class="border-border bg-card rounded-xl border px-4 py-3.5">
          <app-query-chip-bar
            [group]="builderGroup()"
            [catalog]="catalog()"
            i18n-summaryPrefix="
              Prefix of the plain-language summary of an automation's conditions
            "
            summaryPrefix="Continues only when"
            [emptySummary]="emptySummary"
            (groupChange)="setGroup($event)" />
        </div>
      } @else {
        <div
          class="border-border bg-foreground/2 rounded-lg border border-dashed p-4">
          <p class="mb-1 text-sm font-medium">
            <span i18n="Shown when a rule has no conditions">
              Every matching task will run
            </span>
          </p>
          <p class="text-foreground/60 mb-3 text-sm">
            <span i18n="Advises when conditions are needed">
              Add conditions only when this automation should apply to a smaller
              set of tasks.
            </span>
          </p>
          <button
            app-stroked-button
            type="button"
            (click)="addConditionGroup()">
            <span i18n="Button that starts adding conditions">
              Add conditions
            </span>
          </button>
        </div>
      }
    </div>
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
  // "Add conditions" is to write one.
  addConditionGroup() {
    const group = emptyQueryBuilderGroup();
    const field = this.catalog().fields[0];

    this.setGroup(
      field ? { ...group, conditions: [newQueryCondition(field)] } : group
    );
  }
}
