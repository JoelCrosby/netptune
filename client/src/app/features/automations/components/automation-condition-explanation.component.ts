import { Component, computed, forwardRef, input } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { Status, StatusCategory } from '@core/models/status';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
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
          <span
            class="text-primary text-xs font-medium"
            i18n="Marks a condition that the task satisfied">
            matched
          </span>
        } @else {
          <span
            class="text-muted text-xs font-medium"
            i18n="Marks a condition that the task did not satisfy">
            not matched
          </span>
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
                  <span>"{{ displayValue(condition, condition.value) }}"</span>
                }
              </span>

              @if (!condition.isEvaluable) {
                <span class="text-muted text-xs">
                  <span
                    i18n="
                      Marks a condition that only applies while a task is
                      changing
                    ">
                    needs a task change to evaluate
                  </span>
                </span>
              } @else if (condition.isMatch) {
                <span
                  class="text-primary text-xs"
                  i18n="Marks a condition that the task satisfied">
                  matched
                </span>
              } @else {
                <span class="text-warn flex items-center gap-1 text-xs">
                  <span i18n="Label before the value a condition actually saw">
                    actual:
                  </span>
                  @let actualStatus =
                    conditionStatus(condition, condition.actualValue);
                  @if (actualStatus) {
                    <app-task-status-pill
                      [name]="actualStatus.name"
                      [color]="actualStatus.color"
                      [category]="actualStatus.category" />
                  } @else {
                    <span>
                      {{ displayValue(condition, condition.actualValue) }}
                    </span>
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
          [statuses]="statuses()"
          [sprints]="sprints()"
          [users]="users()" />
      }
    </div>
  `,
})
export class AutomationConditionExplanationComponent {
  readonly group = input.required<AutomationConditionGroupExplanation>();
  readonly statuses = input<Status[]>([]);
  readonly sprints = input<SprintViewModel[]>([]);
  readonly users = input<WorkspaceAppUser[]>([]);

  readonly groupLabel = computed(() => {
    return conditionGroupOperatorLabels[this.group().operator];
  });

  describe(condition: AutomationConditionExplanation): string {
    const field = taskChangeFieldLabels[condition.field];
    const operator = conditionOperatorLabels[condition.operator];

    return `${field} ${operator}`;
  }

  displayValue(
    condition: AutomationConditionExplanation,
    value: string | null
  ): string {
    if (!value) return 'empty';

    if (condition.field === TaskChangeField.sprint) {
      return this.sprintName(value);
    }

    if (condition.field === TaskChangeField.assignees) {
      return this.userNames(value);
    }

    return value;
  }

  private sprintName(value: string): string {
    const sprintId = Number(value);
    const sprint = this.sprints().find((candidate) => {
      return candidate.id === sprintId;
    });

    return sprint?.name ?? value;
  }

  private userNames(value: string): string {
    const userIds = value.split(',').map((userId) => userId.trim());
    const names = userIds.map((userId) => {
      const user = this.users().find((candidate) => candidate.id === userId);

      return user?.displayName ?? userId;
    });

    return names.join(', ');
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
