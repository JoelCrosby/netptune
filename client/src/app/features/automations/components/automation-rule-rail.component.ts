import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Status } from '@core/models/status';
import { LucideListFilter, LucideZap } from '@lucide/angular';
import {
  actionTypeLabels,
  countAutomationConditions,
  describeAutomationActionSegments,
  toTextSegments,
  triggerTypeLabels,
} from '@core/automations/automation-copy';
import {
  automationActionKeyword,
  describeConditionsStepSummary,
  describeConditionsStepTitle,
  describeTriggerStepSummary,
} from '@core/automations/automation-flow-copy';
import {
  AutomationAction,
  AutomationTrigger,
} from '@core/automations/automation.models';
import { FlowConnectorComponent } from '@static/components/flow-connector/flow-connector.component';
import { FlowStepComponent } from '@static/components/flow-step/flow-step.component';
import {
  SegmentTextComponent,
  TextSegment,
} from '@static/components/segment-text/segment-text.component';

@Component({
  selector: 'app-automation-rule-rail',
  imports: [
    FlowConnectorComponent,
    FlowStepComponent,
    RouterLink,
    SegmentTextComponent,
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

    <app-flow-step
      i18n-keyword="Heading of the trigger part of the rule"
      keyword="WHEN"
      size="compact"
      [interactive]="false"
      [icon]="triggerIcon"
      [title]="triggerTitle()">
      <app-segment-text [segments]="triggerSummary()" />
    </app-flow-step>

    <app-flow-connector size="compact" />

    <app-flow-step
      i18n-keyword="Heading of the conditions part of the rule"
      keyword="IF"
      size="compact"
      [interactive]="false"
      [icon]="conditionsIcon"
      [title]="conditionsTitle()">
      @if (!hasConditions()) {
        <span
          flowStepAside
          class="text-foreground/45 text-xs"
          i18n="Marks the conditions section as not required">
          Optional
        </span>
      }
      <app-segment-text [segments]="conditionsSummary()" />
    </app-flow-step>

    @for (action of actions(); track $index; let index = $index) {
      <app-flow-connector size="compact" />

      <app-flow-step
        size="compact"
        [interactive]="false"
        [keyword]="actionKeyword(index)"
        [step]="index + 1"
        [title]="actionTitle(action)">
        <app-segment-text [segments]="actionSummary(action)" />
      </app-flow-step>
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
    const segments = describeTriggerStepSummary(this.trigger());

    return toTextSegments(segments, this.statuses());
  });

  protected readonly hasConditions = computed(() => {
    return countAutomationConditions(this.trigger().conditionGroup) > 0;
  });

  protected readonly conditionsTitle = computed(() => {
    return describeConditionsStepTitle(this.trigger());
  });

  protected readonly conditionsSummary = computed(() => {
    const statuses = this.statuses();
    const segments = describeConditionsStepSummary(this.trigger());

    return toTextSegments(segments, statuses);
  });

  actionKeyword(index: number): string {
    return automationActionKeyword(index);
  }

  actionTitle(action: AutomationAction): string {
    return actionTypeLabels[action.type];
  }

  actionSummary(action: AutomationAction): TextSegment[] {
    const statuses = this.statuses();
    const segments = describeAutomationActionSegments(action, statuses);

    return toTextSegments(segments, statuses);
  }
}
