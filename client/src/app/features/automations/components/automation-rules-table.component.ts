import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideCirclePause,
  LucideCirclePlay,
  LucidePencil,
  LucideTrash2,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import {
  automationRunStatusLabels,
  describeAutomationActions,
  describeAutomationTrigger,
  runStatusClass,
} from '../models/automation-copy';
import {
  AutomationRuleListItem,
  AutomationRunStatus,
} from '../models/automation.models';
import { AutomationEnabledBadgeComponent } from './automation-enabled-badge.component';

@Component({
  selector: 'app-automation-rules-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    IconButtonComponent,
    PrettyDatePipe,
    AutomationEnabledBadgeComponent,
    LucideCirclePause,
    LucideCirclePlay,
    LucidePencil,
    LucideTrash2,
  ],
  template: `
    <div class="border-border overflow-auto rounded border">
      <table class="w-full text-sm">
        <thead class="bg-background border-border border-b">
          <tr class="text-left text-xs font-medium tracking-wide uppercase">
            <th class="px-4 py-3">Rule</th>
            <th class="px-4 py-3">Trigger</th>
            <th class="px-4 py-3">Actions</th>
            <th class="px-4 py-3">Last Run</th>
            <th class="px-4 py-3">Status</th>
            @if (canManage()) {
              <th class="w-36 px-4 py-3 text-right">Actions</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (rule of rules(); track rule.id) {
            <tr
              class="border-border hover:bg-foreground/5 border-b transition-colors">
              <td class="min-w-56 px-4 py-3 align-top">
                <a
                  class="font-semibold hover:underline"
                  [routerLink]="[rule.id]">
                  {{ rule.name }}
                </a>
                <div class="mt-1 flex items-center gap-2">
                  <app-automation-enabled-badge [enabled]="rule.isEnabled" />
                </div>
              </td>
              <td class="text-foreground/80 min-w-64 px-4 py-3 align-top">
                {{ triggerSummary(rule) }}
              </td>
              <td class="text-foreground/70 min-w-72 px-4 py-3 align-top">
                {{ actionsSummary(rule) }}
              </td>
              <td
                class="text-foreground/70 px-4 py-3 align-top font-mono text-xs whitespace-nowrap">
                @if (rule.lastRun) {
                  {{ rule.lastRun.createdAt | prettyDate }}
                } @else {
                  Not run yet
                }
              </td>
              <td class="px-4 py-3 align-top">
                @if (rule.lastRun) {
                  <span
                    [class]="
                      'rounded px-2 py-0.5 text-xs font-medium ' +
                      runStatusClass(rule.lastRun.status)
                    ">
                    {{ runStatusLabel(rule.lastRun.status) }}
                  </span>
                } @else {
                  <span class="text-muted text-xs">No runs</span>
                }
              </td>
              @if (canManage()) {
                <td class="px-4 py-2 text-right align-top">
                  <div class="flex justify-end gap-1">
                    <button
                      app-icon-button
                      type="button"
                      [title]="rule.isEnabled ? 'Disable rule' : 'Enable rule'"
                      [disabled]="busyId() === rule.id"
                      (click)="toggleRule.emit(rule)">
                      @if (rule.isEnabled) {
                        <svg lucideCirclePause class="h-4 w-4"></svg>
                      } @else {
                        <svg lucideCirclePlay class="h-4 w-4"></svg>
                      }
                    </button>
                    <button
                      app-icon-button
                      type="button"
                      title="Edit rule"
                      (click)="editRule.emit(rule)">
                      <svg lucidePencil class="h-4 w-4"></svg>
                    </button>
                    <button
                      app-icon-button
                      type="button"
                      title="Delete rule"
                      [disabled]="busyId() === rule.id"
                      (click)="deleteRule.emit(rule)">
                      <svg lucideTrash2 class="h-4 w-4"></svg>
                    </button>
                  </div>
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class AutomationRulesTableComponent {
  readonly rules = input.required<AutomationRuleListItem[]>();
  readonly canManage = input.required<boolean>();
  readonly busyId = input<number | null>(null);

  readonly toggleRule = output<AutomationRuleListItem>();
  readonly editRule = output<AutomationRuleListItem>();
  readonly deleteRule = output<AutomationRuleListItem>();

  triggerSummary(rule: AutomationRuleListItem): string {
    return describeAutomationTrigger(rule.trigger);
  }

  actionsSummary(rule: AutomationRuleListItem): string {
    return describeAutomationActions(rule.actions);
  }

  runStatusLabel(status: AutomationRunStatus): string {
    return automationRunStatusLabels[status];
  }

  runStatusClass(status: AutomationRunStatus): string {
    return runStatusClass(status);
  }
}
