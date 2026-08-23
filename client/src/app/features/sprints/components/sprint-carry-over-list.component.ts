import { Component, computed, input } from '@angular/core';
import { EstimateType, estimateTypeUnits } from '@core/enums/estimate-type';
import { StatusCategory } from '@core/models/status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import {
  numericEstimateType,
  sumTaskEstimates,
} from '@core/tasks/task-estimates';
import { ListGroupHeaderComponent } from '@static/components/list-group-header.component';
import { TaskCompactRowComponent } from '@static/components/task-compact-row.component';

interface CarryOverGroup {
  category: StatusCategory;
  label: string;
  tasks: TaskViewModel[];
}

@Component({
  selector: 'app-sprint-carry-over-list',
  imports: [ListGroupHeaderComponent, TaskCompactRowComponent],
  host: { class: 'flex flex-col gap-3' },
  template: `
    <p class="text-muted text-sm">
      <ng-container i18n="Count of unfinished tasks when completing a sprint">
        {tasks().length, plural,
          =1 {<strong class="text-foreground">1</strong> incomplete task}
          other {
            <strong class="text-foreground">{{ tasks().length }}</strong>
            incomplete tasks
          }
        }
      </ng-container>
      @if (remainingLabel(); as remaining) {
        <span>&nbsp;·&nbsp;{{ remaining }}</span>
      }
    </p>

    <div
      class="border-border custom-scroll max-h-80 overflow-y-auto rounded-lg border">
      @for (group of groups(); track group.category) {
        <app-list-group-header
          [label]="group.label"
          [count]="group.tasks.length" />

        @for (task of group.tasks; track task.id) {
          <app-task-compact-row
            class="border-border border-b last:border-0"
            [task]="task" />
        }
      }
    </div>
  `,
})
export class SprintCarryOverListComponent {
  readonly tasks = input.required<TaskViewModel[]>();
  readonly estimateType = input<EstimateType | null>(null);

  protected readonly groups = computed<CarryOverGroup[]>(() => {
    const tasks = this.tasks();

    return groupOrder
      .map((category) => {
        return {
          category,
          label: groupLabel(category),
          tasks: tasks.filter((task) => task.statusCategory === category),
        };
      })
      .filter((group) => group.tasks.length > 0);
  });

  protected readonly remainingLabel = computed(() => {
    const type = numericEstimateType(this.estimateType());
    const remaining = sumTaskEstimates(this.tasks(), type);

    if (type === null || remaining === 0) return null;

    const amount = `${remaining}${estimateTypeUnits[type]}`;

    return $localize`:Estimate still open when completing a sprint. AMOUNT is a total such as 26pts:${amount}:AMOUNT: remaining`;
  });
}

const groupOrder: StatusCategory[] = [
  StatusCategory.active,
  StatusCategory.todo,
  StatusCategory.new,
  StatusCategory.backlog,
  StatusCategory.inactive,
];

function groupLabel(category: StatusCategory): string {
  switch (category) {
    case StatusCategory.active:
      return $localize`:Heading above the in-progress tasks leaving a sprint:In progress`;
    case StatusCategory.todo:
      return $localize`:Heading above the not-started tasks leaving a sprint:To do`;
    case StatusCategory.new:
      return $localize`:Heading above the newly created tasks leaving a sprint:New`;
    case StatusCategory.backlog:
      return $localize`:Heading above the backlog tasks leaving a sprint:Backlog`;
    default:
      return $localize`:Heading above the inactive tasks leaving a sprint:Inactive`;
  }
}
