import { Component, computed, input } from '@angular/core';
import {
  actionTypeLabels,
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
import { AiChangeField } from '@core/models/ai-conversation';
import { EntityType } from '@core/models/entity-type';
import { Status, StatusCategory } from '@core/models/status';
import {
  LucideListFilter,
  LucideZap,
  type LucideIconInput,
} from '@lucide/angular';
import {
  BadgeColor,
  BadgeComponent,
} from '@static/components/badge/badge.component';
import { FlowConnectorComponent } from '@static/components/flow-connector/flow-connector.component';
import {
  FlowStepChange,
  FlowStepComponent,
} from '@static/components/flow-step/flow-step.component';
import {
  SegmentTextComponent,
  TextSegment,
} from '@static/components/segment-text/segment-text.component';
import { DiffOp, diffSequence } from '@static/components/text-diff/text-diff';

interface AutomationSnapshotStatus {
  id: number;
  name: string;
  color?: string | null;
  category: StatusCategory;
}

interface AutomationSnapshot {
  trigger: AutomationTrigger;
  actions: AutomationAction[];
  statuses: AutomationSnapshotStatus[];
}

interface AutomationActionPlacement {
  action: AutomationAction;
  index: number;
  change: FlowStepChange | null;
  changedFrom?: AutomationAction | null;
  step?: number;
}

interface AutomationDiffStep {
  key: string;
  keyword: string;
  title: string;
  icon: LucideIconInput | null;
  step: number | null;
  change: FlowStepChange | null;
  segments: TextSegment[];
  previous: TextSegment[] | null;
}

const changeLabels: Record<FlowStepChange, string> = {
  added: $localize`:Marks a step a proposed rule change adds:Added`,
  removed: $localize`:Marks a step a proposed rule change removes:Removed`,
  changed: $localize`:Marks a step a proposed rule change alters:Changed`,
};

const changeColors: Record<FlowStepChange, BadgeColor> = {
  added: 'success',
  removed: 'warn',
  changed: 'info',
};

const toStatus = (status: AutomationSnapshotStatus): Status => {
  return {
    ...status,
    workspaceId: 0,
    entityType: EntityType.task,
    key: '',
    sortOrder: 0,
    isSystem: false,
    taskCount: 0,
  };
};

const parseSnapshot = (
  value: string | null | undefined
): AutomationSnapshot | null => {
  if (!value) return null;

  try {
    return JSON.parse(value) as AutomationSnapshot;
  } catch {
    return null;
  }
};

const eventKey = (trigger: AutomationTrigger): string => {
  return JSON.stringify({ ...trigger, conditionGroup: null });
};

const conditionsKey = (trigger: AutomationTrigger): string => {
  return JSON.stringify(trigger.conditionGroup ?? null);
};

/** A proposed automation rule drawn as its When → If → Then flow, each step marked by what the change does to it. */
@Component({
  selector: 'app-ai-assistant-automation-diff',
  host: { class: 'block p-3' },
  imports: [
    BadgeComponent,
    FlowConnectorComponent,
    FlowStepComponent,
    SegmentTextComponent,
  ],
  template: `
    @if (isNew()) {
      <app-badge color="success" class="mb-2">
        <span i18n="Marks a proposed automation rule that does not exist yet">
          New rule
        </span>
      </app-badge>
    }

    @for (step of steps(); track step.key; let first = $first) {
      @if (!first) {
        <app-flow-connector size="compact" />
      }

      <app-flow-step
        size="compact"
        [interactive]="false"
        [keyword]="step.keyword"
        [title]="step.title"
        [icon]="step.icon"
        [step]="step.step"
        [change]="step.change">
        @if (step.change; as change) {
          <app-badge flowStepAside [color]="changeColor(change)">
            {{ changeLabel(change) }}
          </app-badge>
        }
        <app-segment-text [segments]="step.segments" />
        @if (step.previous; as previous) {
          <span class="mt-1 block">
            <app-segment-text struck [segments]="previous" />
          </span>
        }
      </app-flow-step>
    } @empty {
      <span
        class="text-sm"
        i18n="Shown when a proposed automation rule cannot be drawn">
        This rule could not be shown.
      </span>
    }
  `,
})
export class AiAssistantAutomationDiffComponent {
  readonly field = input.required<AiChangeField>();

  private readonly triggerIcon = LucideZap;
  private readonly conditionsIcon = LucideListFilter;

  private readonly before = computed(() => parseSnapshot(this.field().before));
  private readonly after = computed(() => parseSnapshot(this.field().after));

  protected readonly isNew = computed(() => {
    return this.before() === null && this.after() !== null;
  });

  protected readonly steps = computed<AutomationDiffStep[]>(() => {
    const after = this.after();

    if (!after) return [];

    const before = this.before();
    const statuses = after.statuses.map(toStatus);

    return [
      this.triggerStep(before, after, statuses),
      this.conditionsStep(before, after, statuses),
      ...this.actionSteps(before, after, statuses),
    ];
  });

  protected changeLabel(change: FlowStepChange): string {
    return changeLabels[change];
  }

  protected changeColor(change: FlowStepChange): BadgeColor {
    return changeColors[change];
  }

  private triggerStep(
    before: AutomationSnapshot | null,
    after: AutomationSnapshot,
    statuses: Status[]
  ): AutomationDiffStep {
    const isChanged =
      before !== null && eventKey(before.trigger) !== eventKey(after.trigger);
    const summary = describeTriggerStepSummary(after.trigger);
    const previous =
      isChanged && before ? describeTriggerStepSummary(before.trigger) : null;

    return {
      key: 'trigger',
      keyword: $localize`:Heading of the trigger part of the rule:WHEN`,
      title: triggerTypeLabels[after.trigger.type],
      icon: this.triggerIcon,
      step: null,
      change: isChanged ? 'changed' : null,
      segments: toTextSegments(summary, statuses),
      previous: previous ? toTextSegments(previous, statuses) : null,
    };
  }

  private conditionsStep(
    before: AutomationSnapshot | null,
    after: AutomationSnapshot,
    statuses: Status[]
  ): AutomationDiffStep {
    const isChanged =
      before !== null &&
      conditionsKey(before.trigger) !== conditionsKey(after.trigger);
    const summary = describeConditionsStepSummary(after.trigger, statuses);
    const previous =
      isChanged && before
        ? describeConditionsStepSummary(before.trigger, statuses)
        : null;

    return {
      key: 'conditions',
      keyword: $localize`:Heading of the conditions part of the rule:IF`,
      title: describeConditionsStepTitle(after.trigger),
      icon: this.conditionsIcon,
      step: null,
      change: isChanged ? 'changed' : null,
      segments: toTextSegments(summary, statuses),
      previous: previous ? toTextSegments(previous, statuses) : null,
    };
  }

  private actionSteps(
    before: AutomationSnapshot | null,
    after: AutomationSnapshot,
    statuses: Status[]
  ): AutomationDiffStep[] {
    const beforeActions = before?.actions ?? [];
    const ops = before
      ? diffSequence(
          beforeActions.map((action) => String(action.type)),
          after.actions.map((action) => String(action.type))
        )
      : after.actions.map((action): DiffOp => {
          return { kind: 'context', value: String(action.type) };
        });

    const steps: AutomationDiffStep[] = [];
    let beforeIndex = 0;
    let afterIndex = 0;

    for (const op of ops) {
      const index = steps.length;

      if (op.kind === 'removed') {
        const removed = beforeActions[beforeIndex];

        steps.push(
          this.actionStep(
            { action: removed, index, change: 'removed' },
            statuses
          )
        );
        beforeIndex++;

        continue;
      }

      const action = after.actions[afterIndex];
      const isPaired = op.kind === 'context';
      const previous = isPaired ? beforeActions[beforeIndex] : null;
      const change = this.actionChange(op, action, previous, before !== null);
      const changedFrom = change === 'changed' ? previous : null;

      afterIndex++;
      steps.push(
        this.actionStep(
          { action, index, change, changedFrom, step: afterIndex },
          statuses
        )
      );

      if (isPaired) {
        beforeIndex++;
      }
    }

    return steps;
  }

  private actionChange(
    op: DiffOp,
    action: AutomationAction,
    previous: AutomationAction | null,
    hasBefore: boolean
  ): FlowStepChange | null {
    if (!hasBefore) return null;

    if (op.kind === 'added') return 'added';

    const isSame = JSON.stringify(action) === JSON.stringify(previous);

    return isSame ? null : 'changed';
  }

  private actionStep(
    placement: AutomationActionPlacement,
    statuses: Status[]
  ): AutomationDiffStep {
    const { action, index, change, changedFrom, step } = placement;
    const summary = describeAutomationActionSegments(action, statuses);
    const previous = changedFrom
      ? describeAutomationActionSegments(changedFrom, statuses)
      : null;

    return {
      key: `action-${index}`,
      keyword: automationActionKeyword(index),
      title: actionTypeLabels[action.type],
      icon: null,
      step: step ?? null,
      change,
      segments: toTextSegments(summary, statuses),
      previous: previous ? toTextSegments(previous, statuses) : null,
    };
  }
}
