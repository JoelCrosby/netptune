import { Component, computed, forwardRef, input } from '@angular/core';
import { Status, StatusCategory } from '@core/models/status';
import { TaskStatusPillComponent } from '@static/components/task-status-pill.component';
import {
  conditionGroupOperatorLabels,
  conditionOperatorLabels,
  statusLabel,
  taskChangeFieldLabels,
} from '../models/automation-copy';
import {
  AutomationConditionExplanation,
  AutomationConditionGroupExplanation,
  TaskChangeField,
} from '../models/automation.models';

interface ConditionStatus {
  name: string;
  color: string | null;
  category: StatusCategory | null;
}

@Component({
  selector: 'app-automation-condition-explanation',
  imports: [
    forwardRef(() => AutomationConditionExplanationComponent),
    TaskStatusPillComponent,
  ],
  template: `
    <div class="border-border flex flex-col gap-2 rounded-md border p-3">
      <div class="flex items-center gap-2">
        <h5 class="text-xs font-bold tracking-wider">{{ groupLabel() }}</h5>
        @if (group().isMatch) {
          <span class="text-primary text-xs font-medium">matched</span>
        } @else {
          <span class="text-muted text-xs font-medium">not matched</span>
        }
      </div>

      @if (group().conditions.length) {
        <ul class="flex flex-col gap-1">
          @for (condition of group().conditions; track $index) {
            <li class="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm">
              <span
                class="flex items-center gap-1"
                [class.text-muted]="!condition.isMatch">
                <span>{{ describe(condition) }}</span>
                @let expectedStatus =
                  conditionStatus(condition, condition.value);
                @if (expectedStatus) {
                  <app-task-status-pill
                    [name]="expectedStatus.name"
                    [color]="expectedStatus.color"
                    [category]="expectedStatus.category" />
                } @else if (condition.value) {
                  <span>"{{ condition.value }}"</span>
                }
              </span>

              @if (!condition.isEvaluable) {
                <span class="text-muted text-xs">
                  needs a task change to evaluate
                </span>
              } @else if (condition.isMatch) {
                <span class="text-primary text-xs">matched</span>
              } @else {
                <span class="text-warn flex items-center gap-1 text-xs">
                  <span>actual:</span>
                  @let actualStatus =
                    conditionStatus(condition, condition.actualValue);
                  @if (actualStatus) {
                    <app-task-status-pill
                      [name]="actualStatus.name"
                      [color]="actualStatus.color"
                      [category]="actualStatus.category" />
                  } @else {
                    <span>{{ condition.actualValue ?? 'empty' }}</span>
                  }
                </span>
              }
            </li>
          }
        </ul>
      }

      @for (nested of group().groups; track $index) {
        <app-automation-condition-explanation
          [group]="nested"
          [statuses]="statuses()" />
      }
    </div>
  `,
})
export class AutomationConditionExplanationComponent {
  readonly group = input.required<AutomationConditionGroupExplanation>();
  readonly statuses = input<Status[]>([]);

  readonly groupLabel = computed(() => {
    return conditionGroupOperatorLabels[this.group().operator];
  });

  describe(condition: AutomationConditionExplanation): string {
    const field = taskChangeFieldLabels[condition.field];
    const operator = conditionOperatorLabels[condition.operator];

    return `${field} ${operator}`;
  }

  conditionStatus(
    condition: AutomationConditionExplanation,
    value: string | null
  ): ConditionStatus | null {
    if (condition.field !== TaskChangeField.status) return null;

    if (!value) return null;

    const statusId = Number(value);

    if (!Number.isInteger(statusId)) return null;

    const status = this.statuses().find((candidate) => {
      return candidate.id === statusId;
    });

    if (!status) {
      return {
        name: statusLabel(statusId, this.statuses()),
        color: null,
        category: null,
      };
    }

    return {
      name: status.name,
      color: status.color ?? null,
      category: status.category,
    };
  }
}
