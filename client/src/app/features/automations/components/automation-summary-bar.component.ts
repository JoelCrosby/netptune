import { Component, computed, input, model } from '@angular/core';
import { Status } from '@core/models/status';
import { LucideChevronDown } from '@lucide/angular';
import {
  AutomationCopySegment,
  describeAutomationActionsSegments,
  describeAutomationConditionsSegments,
  describeAutomationOneLine,
  describeAutomationTriggerSegments,
} from '../models/automation-copy';
import {
  AutomationAction,
  AutomationTrigger,
  AutomationTriggerType,
} from '../models/automation.models';
import { AutomationDescriptionComponent } from './automation-description.component';

@Component({
  selector: 'app-automation-summary-bar',
  imports: [AutomationDescriptionComponent, LucideChevronDown],
  host: {
    class:
      'border-border bg-card sticky top-3 z-5 block rounded-lg border shadow-sm',
  },
  template: `
    <div class="flex items-center gap-3 px-3.5 py-2.5">
      <p class="text-foreground/75 min-w-0 flex-1 text-sm text-pretty">
        {{ oneLine() }}
      </p>

      <button
        type="button"
        class="text-foreground/60 hover:bg-foreground/5 focus-visible:ring-primary inline-flex h-7.5 shrink-0 items-center gap-1.5 rounded-lg px-2.5 text-[13px] font-semibold transition-colors focus-visible:ring-2 focus-visible:outline-none"
        [attr.aria-expanded]="open()"
        (click)="open.set(!open())">
        <span>{{ toggleLabel() }}</span>
        <svg
          lucideChevronDown
          class="h-3.5 w-3.5 transition-transform"
          [class.rotate-180]="open()"></svg>
      </button>
    </div>

    @if (open()) {
      <dl
        class="border-border grid grid-cols-[3.5rem_minmax(0,1fr)] items-baseline gap-x-3.5 gap-y-2.5 border-t p-3.5">
        <dt class="text-primary text-[0.6875rem] font-bold tracking-[0.1em]">
          <span i18n="Heading of the trigger part of the rule">WHEN</span>
        </dt>
        <dd class="m-0 text-sm leading-normal">
          <app-automation-description
            [segments]="triggerSummary()"
            [statuses]="statuses()" />
        </dd>

        <dt class="text-primary text-[0.6875rem] font-bold tracking-[0.1em]">
          <span i18n="Heading of the conditions part of the rule">IF</span>
        </dt>
        <dd class="m-0 text-sm leading-normal">
          <app-automation-description
            [segments]="conditionsSummary()"
            [statuses]="statuses()" />
        </dd>

        <dt class="text-primary text-[0.6875rem] font-bold tracking-[0.1em]">
          <span i18n="Heading of the actions part of the rule">THEN</span>
        </dt>
        <dd class="m-0 text-sm leading-normal">
          <app-automation-description
            [segments]="actionsSummary()"
            [statuses]="statuses()" />
        </dd>
      </dl>
    }
  `,
})
export class AutomationSummaryBarComponent {
  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly statuses = input<Status[]>([]);
  readonly open = model(true);

  protected readonly oneLine = computed(() => {
    return describeAutomationOneLine(this.trigger(), this.actions());
  });

  protected readonly toggleLabel = computed(() => {
    return this.open()
      ? $localize`:Hides the expanded automation summary:Hide detail`
      : $localize`:Reveals the expanded automation summary:Show detail`;
  });

  // The trigger line describes the event alone, because the conditions have their own row.
  protected readonly triggerSummary = computed<AutomationCopySegment[]>(() => {
    const trigger = this.trigger();
    const withoutConditions =
      trigger.type === AutomationTriggerType.taskChanged
        ? { ...trigger, conditionGroup: null }
        : trigger;

    return describeAutomationTriggerSegments(
      withoutConditions,
      this.statuses()
    );
  });

  protected readonly conditionsSummary = computed<AutomationCopySegment[]>(
    () => {
      return describeAutomationConditionsSegments(
        this.trigger(),
        this.statuses()
      );
    }
  );

  protected readonly actionsSummary = computed<AutomationCopySegment[]>(() => {
    return describeAutomationActionsSegments(this.actions(), this.statuses());
  });
}
