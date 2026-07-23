import { Component, input } from '@angular/core';
import { Status } from '@core/models/status';
import {
  LucideEye,
  LucideListFilter,
  LucideListOrdered,
  LucideZap,
} from '@lucide/angular';
import {
  AutomationCopySegment,
  describeAutomationActionSegments,
  describeAutomationConditionsSegments,
  describeAutomationTriggerSegments,
} from '../models/automation-copy';
import {
  AutomationAction,
  AutomationTrigger,
  AutomationTriggerType,
} from '../models/automation.models';
import { AutomationDescriptionComponent } from './automation-description.component';

@Component({
  selector: 'app-automation-rule-summary',
  imports: [
    AutomationDescriptionComponent,
    LucideEye,
    LucideListFilter,
    LucideListOrdered,
    LucideZap,
  ],
  template: `
    <section
      class="border-border bg-background overflow-hidden rounded-lg border shadow-sm"
      aria-label="Rule preview">
      <div
        class="border-border bg-foreground/3 flex items-center justify-between gap-3 border-b px-4 py-3">
        <div class="flex min-w-0 items-center gap-3">
          <span
            class="bg-primary/10 text-primary flex h-8 w-8 shrink-0 items-center justify-center rounded-full">
            <svg lucideEye class="h-4 w-4" aria-hidden="true"></svg>
          </span>
          <div class="min-w-0">
            <h2 class="font-overpass text-base font-medium">Rule preview</h2>
            <p class="text-foreground/60 truncate text-xs">When → If → Then</p>
          </div>
        </div>
        <span
          class="bg-primary/10 text-primary rounded-full px-2 py-1 text-[0.65rem] font-bold tracking-wider">
          LIVE
        </span>
      </div>

      <div class="flex flex-col p-3">
        <div class="flex min-w-0 gap-2">
          <div
            class="relative flex w-8 shrink-0 justify-center"
            aria-hidden="true">
            <div
              class="bg-primary/30 absolute top-8 -bottom-3 left-1/2 w-px"></div>
            <span
              class="bg-primary text-primary-foreground relative flex h-8 w-8 items-center justify-center rounded-full shadow-sm">
              <svg lucideZap class="h-4 w-4"></svg>
            </span>
          </div>
          <article
            class="border-border mb-3 min-w-0 flex-1 overflow-hidden rounded-md border">
            <header
              class="border-border bg-foreground/3 flex items-center gap-2 border-b px-3 py-2">
              <span
                class="bg-primary/10 text-primary flex h-6 w-6 items-center justify-center rounded-full">
                <svg lucideZap class="h-3.5 w-3.5" aria-hidden="true"></svg>
              </span>
              <h3 class="text-xs font-bold tracking-wider">WHEN</h3>
            </header>
            <p class="p-3 text-sm leading-relaxed">
              <app-automation-description
                [segments]="triggerSummary()"
                [statuses]="statuses()" />
            </p>
          </article>
        </div>

        @if (supportsConditions()) {
          <div class="flex min-w-0 gap-2">
            <div
              class="relative flex w-8 shrink-0 justify-center"
              aria-hidden="true">
              <div
                class="bg-primary/30 absolute top-8 -bottom-3 left-1/2 w-px"></div>
              <span
                class="bg-primary text-primary-foreground relative flex h-8 w-8 items-center justify-center rounded-full shadow-sm">
                <svg lucideListFilter class="h-4 w-4"></svg>
              </span>
            </div>
            <article
              class="border-border mb-3 min-w-0 flex-1 overflow-hidden rounded-md border">
              <header
                class="border-border bg-foreground/3 flex items-center justify-between gap-2 border-b px-3 py-2">
                <div class="flex items-center gap-2">
                  <span
                    class="bg-primary/10 text-primary flex h-6 w-6 items-center justify-center rounded-full">
                    <svg
                      lucideListFilter
                      class="h-3.5 w-3.5"
                      aria-hidden="true"></svg>
                  </span>
                  <h3 class="text-xs font-bold tracking-wider">IF</h3>
                </div>
                <span
                  class="text-foreground/45 text-[0.6rem] font-semibold tracking-wide">
                  OPTIONAL
                </span>
              </header>
              <p
                class="p-3 text-sm leading-relaxed"
                [class.text-foreground/55]="!hasConditions()">
                <app-automation-description
                  [segments]="conditionsSummary()"
                  [statuses]="statuses()" />
              </p>
            </article>
          </div>
        }

        <div class="flex min-w-0 gap-2">
          <div class="flex w-8 shrink-0 justify-center" aria-hidden="true">
            <span
              class="bg-primary text-primary-foreground relative flex h-8 w-8 items-center justify-center rounded-full shadow-sm">
              <svg lucideListOrdered class="h-4 w-4"></svg>
            </span>
          </div>
          <article
            class="border-border min-w-0 flex-1 overflow-hidden rounded-md border">
            <header
              class="border-border bg-foreground/3 flex items-center gap-2 border-b px-3 py-2">
              <span
                class="bg-primary/10 text-primary flex h-6 w-6 items-center justify-center rounded-full">
                <svg
                  lucideListOrdered
                  class="h-3.5 w-3.5"
                  aria-hidden="true"></svg>
              </span>
              <h3 class="text-xs font-bold tracking-wider">THEN</h3>
            </header>
            <ol class="flex list-none flex-col gap-2 p-3">
              @for (
                action of actions();
                track $index;
                let actionIndex = $index
              ) {
                <li class="flex min-w-0 items-start gap-2 text-sm">
                  <span
                    class="bg-primary/10 text-primary flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[0.65rem] font-bold">
                    {{ actionIndex + 1 }}
                  </span>
                  <span class="min-w-0 leading-relaxed">
                    <app-automation-description
                      [segments]="actionSummary(action)"
                      [statuses]="statuses()" />
                  </span>
                </li>
              }
            </ol>
          </article>
        </div>
      </div>
    </section>
  `,
})
export class AutomationRuleSummaryComponent {
  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly statuses = input<Status[]>([]);

  triggerSummary(): AutomationCopySegment[] {
    const trigger = this.trigger();

    return describeAutomationTriggerSegments(
      trigger.type === AutomationTriggerType.taskChanged
        ? {
            ...trigger,
            conditions: null,
            conditionGroup: null,
            statusId: null,
            assigneeChangeMode: null,
          }
        : trigger,
      this.statuses()
    );
  }

  supportsConditions(): boolean {
    return this.trigger().type === AutomationTriggerType.taskChanged;
  }

  hasConditions(): boolean {
    const trigger = this.trigger();

    return !!trigger.conditionGroup || !!trigger.conditions?.length;
  }

  conditionsSummary(): AutomationCopySegment[] {
    return describeAutomationConditionsSegments(
      this.trigger(),
      this.statuses()
    );
  }

  actionSummary(action: AutomationAction): AutomationCopySegment[] {
    return describeAutomationActionSegments(action, this.statuses());
  }
}
