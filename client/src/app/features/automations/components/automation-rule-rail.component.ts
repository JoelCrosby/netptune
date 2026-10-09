import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Status } from '@core/models/status';
import { LucideListFilter, LucideZap } from '@lucide/angular';
import {
  actionTypeLabels,
  countAutomationConditions,
  describeAutomationActionSegments,
  triggerTypeLabels,
} from '../models/automation-copy';
import {
  automationActionKeyword,
  describeConditionsStepSummary,
  describeConditionsStepTitle,
  describeTriggerStepSummary,
} from '../models/automation-flow-copy';
import {
  AutomationAction,
  AutomationTrigger,
} from '../models/automation.models';
import { AutomationDescriptionComponent } from './automation-description.component';
import { AutomationFlowNodeComponent } from './automation-flow-node.component';

@Component({
  selector: 'app-automation-rule-rail',
  imports: [
    AutomationDescriptionComponent,
    AutomationFlowNodeComponent,
    RouterLink,
  ],
  host: {
    class: 'border-border bg-foreground/2 block rounded-xl border p-4',
  },
  template: `
    <div class="mb-3 flex items-center justify-between">
      <h2 class="text-[15px] font-bold">
        <span i18n="Heading of the rule summary beside the run history">
          Rule
        </span>
      </h2>

      @if (editLink(); as link) {
        <a
          class="text-primary hover:bg-foreground/5 focus-visible:ring-primary -mr-1.5 inline-flex h-7 items-center rounded-md px-2 text-[13px] font-semibold transition-colors focus-visible:ring-2 focus-visible:outline-none"
          [routerLink]="link"
          i18n="Link that opens the automation editor">
          Edit
        </a>
      }
    </div>

    <app-automation-flow-node
      i18n-keyword="Heading of the trigger part of the rule"
      keyword="WHEN"
      size="compact"
      [interactive]="false"
      [icon]="triggerIcon"
      [title]="triggerTitle()">
      <app-automation-description
        [segments]="triggerSummary()"
        [statuses]="statuses()" />
    </app-automation-flow-node>

    <div class="bg-border ml-5.25 h-2.5 w-px" aria-hidden="true"></div>

    <app-automation-flow-node
      i18n-keyword="Heading of the conditions part of the rule"
      keyword="IF"
      size="compact"
      [interactive]="false"
      [icon]="conditionsIcon"
      [title]="conditionsTitle()">
      @if (!hasConditions()) {
        <span
          flowNodeAside
          class="text-foreground/45 text-xs"
          i18n="Marks the conditions section as not required">
          Optional
        </span>
      }
      <app-automation-description
        [segments]="conditionsSummary()"
        [statuses]="statuses()" />
    </app-automation-flow-node>

    @for (action of actions(); track $index; let index = $index) {
      <div class="bg-border ml-5.25 h-2.5 w-px" aria-hidden="true"></div>

      <app-automation-flow-node
        size="compact"
        [interactive]="false"
        [keyword]="actionKeyword(index)"
        [step]="index + 1"
        [title]="actionTitle(action)">
        <app-automation-description
          [segments]="actionSummary(action)"
          [statuses]="statuses()" />
      </app-automation-flow-node>
    }
  `,
})
export class AutomationRuleRailComponent {
  readonly triggerIcon = LucideZap;
  readonly conditionsIcon = LucideListFilter;

  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly statuses = input<Status[]>([]);
  readonly editLink = input<unknown[] | null>(null);

  protected readonly triggerTitle = computed(() => {
    return triggerTypeLabels[this.trigger().type];
  });

  protected readonly triggerSummary = computed(() => {
    return describeTriggerStepSummary(this.trigger());
  });

  protected readonly hasConditions = computed(() => {
    return countAutomationConditions(this.trigger().conditionGroup) > 0;
  });

  protected readonly conditionsTitle = computed(() => {
    return describeConditionsStepTitle(this.trigger());
  });

  protected readonly conditionsSummary = computed(() => {
    return describeConditionsStepSummary(this.trigger(), this.statuses());
  });

  actionKeyword(index: number): string {
    return automationActionKeyword(index);
  }

  actionTitle(action: AutomationAction): string {
    return actionTypeLabels[action.type];
  }

  actionSummary(action: AutomationAction) {
    return describeAutomationActionSegments(action, this.statuses());
  }
}
