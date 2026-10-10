import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { AiChangeApplyStatus } from '@core/models/ai-conversation';
import {
  LucideChevronDown,
  LucideCircleCheck,
  LucideCircleDashed,
  LucideTriangleAlert,
  LucideUndo2,
} from '@lucide/angular';
import {
  BadgeColor,
  BadgeComponent,
} from '@static/components/badge/badge.component';
import { SpinnerIconComponent } from '@static/components/spinner/spinner-icon.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import {
  AiBatchedGroups,
  AiChangeGroup,
  fieldLabel,
  groupAction,
  groupTarget,
  groupTone,
  isApplied,
  isValid,
  selectableIds,
} from './ai-assistant-change-group';
import {
  changeAction,
  changeKind,
  changeTone,
} from './ai-assistant-change-kind';

/** Where an entity has got to, read from its changes once a run or a record is shown. */
type AiRowState =
  'idle' | 'waiting' | 'running' | 'applied' | 'failed' | 'undone';

interface AiReviewRow {
  key: string;
  title: string;
  systemId: string | null;
  action: string;
  tone: BadgeColor;
  what: string;
  isSelected: boolean;
  isSkipped: boolean;
  isDimmed: boolean;
  blockedMessage: string | null;
  state: AiRowState;
}

interface AiReviewBatch {
  key: string;
  action: string;
  tone: BadgeColor;
  field: string;
  after: string;
  count: string;
  isOpen: boolean;
  rows: AiReviewRow[];
}

@Component({
  selector: 'app-ai-assistant-review-list',
  host: { class: 'block' },
  imports: [
    BadgeComponent,
    NgTemplateOutlet,
    LucideChevronDown,
    LucideCircleCheck,
    LucideCircleDashed,
    LucideTriangleAlert,
    LucideUndo2,
    SpinnerIconComponent,
    TaskScopeIdComponent,
  ],
  template: `
    @for (batch of reviewBatches(); track batch.key) {
      <div class="border-border border-b">
        <button
          type="button"
          class="hover:bg-card-hover flex w-full cursor-pointer items-center gap-2 py-4 pr-3.5 pl-1.5 text-left transition-colors"
          [attr.aria-expanded]="batch.isOpen"
          (click)="groupToggled.emit(batch.key)">
          <svg
            lucideChevronDown
            class="text-muted h-3.5 w-3.5 shrink-0 transition-transform"
            [class.-rotate-90]="!batch.isOpen"></svg>
          <span class="flex min-w-0 flex-1 flex-wrap items-center gap-1.5">
            <app-badge shape="rounded" class="font-bold" [color]="batch.tone">
              {{ batch.action }}
            </app-badge>
            <span
              class="text-muted text-[13px]"
              i18n="
                Joins a field name to the value a batch of changes sets it to,
                as in 'Status to Done'
              "
              >{{ batch.field }} to</span
            >
            <app-badge color="primary" class="max-w-full truncate">
              {{ batch.after }}
            </app-badge>
          </span>
          <span class="text-muted font-avatar shrink-0 text-xs">
            {{ batch.count }}
          </span>
        </button>

        @if (batch.isOpen) {
          <div class="ml-6 flex flex-col gap-2 p-2">
            @for (row of batch.rows; track row.key) {
              <div
                class="hover:bg-hover relative flex cursor-pointer items-center gap-2.5 rounded py-2 pr-3.5 pl-3.5 transition-colors"
                [class.bg-primary/10]="row.isSelected"
                [class.opacity-45]="row.isDimmed"
                [attr.aria-current]="row.isSelected"
                (click)="selected.emit(row.key)">
                <span
                  class="ring-background absolute top-3.25 -left-1.25 h-2 w-2 rounded-full ring-3"
                  [class]="dotClass(row)"></span>
                <ng-container
                  *ngTemplateOutlet="
                    body;
                    context: { $implicit: row, inBatch: true }
                  " />
              </div>
            }
          </div>
        }
      </div>
    }

    @if (reviewBatches().length > 0 && reviewRows().length > 0) {
      <div
        class="text-muted/80 font-avatar px-3.5 pt-3.5 pb-2 text-[10.5px] tracking-widest uppercase"
        i18n="Heading above the changes that are not part of a batch">
        Other changes
      </div>
    }

    @for (row of reviewRows(); track row.key) {
      <div
        class="border-border/50 hover:bg-hover flex cursor-pointer items-start gap-2.5 border-b border-l-2 py-2.5 pr-3.5 pl-3 transition-colors"
        [class]="
          row.isSelected
            ? 'border-l-primary bg-primary/10'
            : 'border-l-transparent'
        "
        [class.opacity-45]="row.isDimmed"
        [attr.aria-current]="row.isSelected"
        (click)="selected.emit(row.key)">
        <ng-container
          *ngTemplateOutlet="
            body;
            context: { $implicit: row, inBatch: false }
          " />
      </div>
    }

    @if (reviewBatches().length === 0 && reviewRows().length === 0) {
      <p
        class="text-muted px-3 py-10 text-center text-sm"
        i18n="Shown when no change matches the review filter">
        No changes match this filter.
      </p>
    }

    <ng-template #body let-row let-inBatch="inBatch">
      <span class="flex min-w-0 flex-1 flex-col">
        <span class="flex min-w-0 items-center gap-2">
          @if (row.systemId; as systemId) {
            <app-task-scope-id class="shrink-0" [id]="systemId" />
          }
          <span class="truncate text-[14px] leading-snug" [title]="row.title">
            {{ row.title }}
          </span>
        </span>
        <span class="flex min-w-0 items-center gap-1.5">
          @if (!inBatch) {
            <app-badge
              shape="rounded"
              class="shrink-0 px-1.5 text-[10.5px] font-bold"
              [color]="row.tone">
              {{ row.action }}
            </app-badge>
          }
          @if (!inBatch && row.what) {
            <span class="text-muted min-w-0 truncate text-[11.5px]">
              {{ row.what }}
            </span>
          }
          @if (row.isSkipped) {
            <span
              class="border-border text-muted shrink-0 rounded-sm border px-1.5 text-[11px]"
              i18n="Marks a change the reviewer left out of the apply">
              Skipped
            </span>
          }
        </span>
        @if (row.blockedMessage; as message) {
          <span class="text-change-removed text-xs">
            <span i18n="Prefix to the reason a change cannot be applied"
              >Can’t apply —</span
            >
            {{ message }}
          </span>
        }
      </span>

      @switch (row.state) {
        @case ('applied') {
          <svg
            lucideCircleCheck
            class="text-change-added mt-0.5 h-4.5 w-4.5 shrink-0"
            [attr.aria-label]="appliedLabel"></svg>
        }
        @case ('failed') {
          <svg
            lucideTriangleAlert
            class="text-change-removed mt-0.5 h-4.5 w-4.5 shrink-0"
            [attr.aria-label]="failedLabel"></svg>
        }
        @case ('running') {
          <app-spinner-icon
            class="mt-0.5 h-4.5 w-4.5 shrink-0"
            [label]="runningLabel" />
        }
        @case ('waiting') {
          <svg
            lucideCircleDashed
            class="text-muted mt-0.5 h-4.5 w-4.5 shrink-0"
            [attr.aria-label]="waitingLabel"></svg>
        }
        @case ('undone') {
          <svg
            lucideUndo2
            class="text-muted mt-0.5 h-4.5 w-4.5 shrink-0"
            [attr.aria-label]="undoneLabel"></svg>
        }
      }
    </ng-template>
  `,
})
export class AiAssistantReviewListComponent {
  readonly groups = input.required<AiBatchedGroups>();
  readonly excludedChangeIds = input.required<Set<number>>();
  readonly collapsedKeys = input.required<ReadonlySet<string>>();
  readonly selectedKey = input<string | null>(null);
  readonly isPending = input(false);
  readonly isApplying = input(false);
  readonly applyStatuses = input<ReadonlyMap<number, AiChangeApplyStatus>>(
    new Map()
  );
  readonly applyingChangeId = input<number | null>(null);

  readonly selected = output<string>();
  readonly groupToggled = output<string>();

  protected readonly appliedLabel = $localize`:Marks a change that has been applied:Applied`;
  protected readonly failedLabel = $localize`:Marks a change that could not be applied:Failed`;
  protected readonly runningLabel = $localize`:Marks the change being applied right now:Applying`;
  protected readonly waitingLabel = $localize`:Marks a change waiting to be applied:Waiting`;
  protected readonly undoneLabel = $localize`:Marks a change that was taken back:Undone`;

  protected readonly reviewBatches = computed<AiReviewBatch[]>(() => {
    const excluded = this.excludedChangeIds();
    const collapsed = this.collapsedKeys();

    return this.groups().batches.map((batch) => {
      const ids = batch.groups.flatMap(selectableIds);
      const included = ids.filter((id) => !excluded.has(id)).length;
      const total = batch.groups.length;
      const count =
        included === total
          ? `${total}`
          : $localize`:Counts the changes in a batch that will be applied:${included}:INCLUDED: of ${total}:TOTAL:`;

      return {
        key: batch.key,
        action: changeAction(batch.sample),
        tone: changeTone(batch.sample),
        field: fieldLabel(batch.field.name),
        after: batch.field.after ?? '',
        count,
        isOpen: !collapsed.has(batch.key),
        rows: batch.groups.map((group) => this.row(group)),
      };
    });
  });

  protected readonly reviewRows = computed<AiReviewRow[]>(() => {
    return this.groups().rest.map((group) => this.row(group));
  });

  protected dotClass(row: AiReviewRow): string {
    if (row.blockedMessage || row.state === 'failed') {
      return 'bg-change-removed';
    }

    if (row.isSelected) {
      return 'bg-primary';
    }

    if (row.isSkipped) {
      return 'bg-foreground/30';
    }

    return 'bg-change-added';
  }

  private row(group: AiChangeGroup): AiReviewRow {
    const excluded = this.excludedChangeIds();
    const ids = selectableIds(group);
    const included = ids.filter((id) => !excluded.has(id));
    const blocked = group.changes.find((change) => !isValid(change));
    const isSkipped = ids.length > 0 && included.length === 0;
    const isUndone = group.changes.every((change) => change.undoneAt);
    const state = this.state(group, included);

    return {
      key: group.key,
      title: groupTarget(group),
      systemId: group.changes[0].entitySystemId ?? null,
      action: groupAction(group),
      tone: groupTone(group),
      what: this.what(group),
      isSelected: group.key === this.selectedKey(),
      isSkipped: this.isPending() && isSkipped,
      isDimmed:
        (this.isPending() && !this.isApplying() && isSkipped && !blocked) ||
        state === 'waiting' ||
        isUndone,
      blockedMessage: blocked ? this.blockedMessage(blocked) : null,
      state,
    };
  }

  /** Field names say what an update touches; a creation or a deletion says it in its badge. */
  private what(group: AiChangeGroup): string {
    const kinds = group.changes.map(changeKind);

    if (kinds.includes('create') || kinds.includes('delete')) {
      return '';
    }

    const names = group.changes.flatMap((change) => {
      return change.fields.map((field) => fieldLabel(field.name));
    });

    return [...new Set(names)].join(', ');
  }

  private blockedMessage(change: AiChangeGroup['changes'][number]): string {
    return (
      change.validationMessage ??
      $localize`:Shown on a proposal that cannot be applied:This change cannot be applied.`
    );
  }

  private state(group: AiChangeGroup, included: number[]): AiRowState {
    if (this.isApplying()) {
      return this.runState(included);
    }

    if (this.isPending()) {
      return 'idle';
    }

    if (group.changes.every((change) => change.undoneAt)) {
      return 'undone';
    }

    const hasFailed = group.changes.some((change) => {
      return change.applyStatus === AiChangeApplyStatus.failed;
    });

    if (hasFailed) {
      return 'failed';
    }

    return group.changes.some(isApplied) ? 'applied' : 'idle';
  }

  private runState(included: number[]): AiRowState {
    if (included.length === 0) {
      return 'idle';
    }

    if (included.includes(this.applyingChangeId() ?? -1)) {
      return 'running';
    }

    const statuses = included.map((id) => this.applyStatuses().get(id));

    if (statuses.some((status) => status === AiChangeApplyStatus.failed)) {
      return 'failed';
    }

    if (statuses.every((status) => status === AiChangeApplyStatus.applied)) {
      return 'applied';
    }

    return 'waiting';
  }
}
