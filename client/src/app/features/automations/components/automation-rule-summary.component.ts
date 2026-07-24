import { Component, input } from '@angular/core';
import { Status } from '@core/models/status';
import {
  LucideEye,
  LucideListFilter,
  LucideListOrdered,
  LucideZap,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { IconCircleComponent } from '@static/components/icon-circle.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
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
    BadgeComponent,
    IconCircleComponent,
    PanelComponent,
    PanelHeaderComponent,
  ],
  template: `
    <app-panel aria-label="Rule preview">
      <app-panel-header
        heading="Rule preview"
        description="When → If → Then"
        [icon]="previewIcon">
        <app-badge
          panelHeaderActions
          color="primary"
          class="text-[0.65rem] font-bold tracking-wider">
          LIVE
        </app-badge>
      </app-panel-header>

      <div class="flex flex-col p-3">
        <div class="flex min-w-0 gap-2">
          <div
            class="relative flex w-8 shrink-0 justify-center"
            aria-hidden="true">
            <div
              class="bg-primary/30 absolute top-8 -bottom-3 left-1/2 w-px"></div>
            <app-icon-circle appearance="solid" [icon]="triggerIcon" />
          </div>
          <article
            class="border-border mb-3 min-w-0 flex-1 overflow-hidden rounded-md border">
            <header
              class="border-border bg-foreground/3 flex items-center gap-2 border-b px-3 py-2">
              <app-icon-circle size="small" [icon]="triggerIcon" />
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
              <app-icon-circle appearance="solid" [icon]="conditionIcon" />
            </div>
            <article
              class="border-border mb-3 min-w-0 flex-1 overflow-hidden rounded-md border">
              <header
                class="border-border bg-foreground/3 flex items-center justify-between gap-2 border-b px-3 py-2">
                <div class="flex items-center gap-2">
                  <app-icon-circle size="small" [icon]="conditionIcon" />
                  <h3 class="text-xs font-bold tracking-wider">IF</h3>
                </div>
                <app-badge
                  class="text-foreground/45 bg-transparent text-[0.6rem] tracking-wide">
                  OPTIONAL
                </app-badge>
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
            <app-icon-circle appearance="solid" [icon]="actionsIcon" />
          </div>
          <article
            class="border-border min-w-0 flex-1 overflow-hidden rounded-md border">
            <header
              class="border-border bg-foreground/3 flex items-center gap-2 border-b px-3 py-2">
              <app-icon-circle size="small" [icon]="actionsIcon" />
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
    </app-panel>
  `,
})
export class AutomationRuleSummaryComponent {
  readonly previewIcon = LucideEye;
  readonly triggerIcon = LucideZap;
  readonly conditionIcon = LucideListFilter;
  readonly actionsIcon = LucideListOrdered;

  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly statuses = input<Status[]>([]);

  triggerSummary(): AutomationCopySegment[] {
    const trigger = this.trigger();

    return describeAutomationTriggerSegments(
      trigger.type === AutomationTriggerType.taskChanged
        ? {
            ...trigger,
            conditionGroup: null,
          }
        : trigger,
      this.statuses()
    );
  }

  supportsConditions(): boolean {
    return this.trigger().type === AutomationTriggerType.taskChanged;
  }

  hasConditions(): boolean {
    return !!this.trigger().conditionGroup;
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
