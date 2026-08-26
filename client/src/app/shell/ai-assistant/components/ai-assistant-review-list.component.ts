import { Component, computed, input, output } from '@angular/core';
import {
  AiChangeApplyStatus,
  AiProposedChange,
} from '@core/models/ai-conversation';
import {
  LucideChevronDown,
  LucideCircleCheck,
  LucideMinus,
  LucideTriangleAlert,
  LucideUndo2,
} from '@lucide/angular';
import { SelectionCheckboxComponent } from '@static/components/checkbox/selection-checkbox.component';
import {
  AiChangeGroup,
  entityLabel,
  isValid,
} from './ai-assistant-change-group';
import { changeSummary } from './ai-assistant-change-kind';
import { AiChangeLetter, changeLetter } from './ai-assistant-diff';

interface AiReviewRow {
  change: AiProposedChange;
  letter: AiChangeLetter;
  title: string;
  detail: string;
  isSelectable: boolean;
  isIncluded: boolean;
  isSelected: boolean;
  isBlocked: boolean;
}

interface AiReviewGroup {
  key: string;
  entity: string;
  title: string;
  count: number;
  isOpen: boolean;
  rows: AiReviewRow[];
}

/** The left hand list of a review: one collapsible section per entity, one row per change. */
@Component({
  selector: 'app-ai-assistant-review-list',
  host: { class: 'block' },
  imports: [
    LucideChevronDown,
    LucideCircleCheck,
    LucideMinus,
    LucideTriangleAlert,
    LucideUndo2,
    SelectionCheckboxComponent,
  ],
  template: `
    @for (group of reviewGroups(); track group.key) {
      <div>
        <button
          type="button"
          class="border-border/60 bg-card hover:bg-card-hover flex w-full items-center gap-2 border-t px-3 py-2.5 text-left text-[15px] transition-colors"
          [attr.aria-expanded]="group.isOpen"
          (click)="groupToggled.emit(group.key)">
          <svg
            lucideChevronDown
            class="text-muted h-3.5 w-3.5 shrink-0 transition-transform"
            [class.-rotate-90]="!group.isOpen"></svg>
          <span class="text-muted shrink-0 text-xs tracking-wide uppercase">
            {{ group.entity }}
          </span>
          <span class="min-w-0 truncate font-medium">{{ group.title }}</span>
          <span class="flex-1"></span>
          <span
            class="bg-foreground/8 text-muted flex h-5 min-w-5 items-center justify-center rounded-full px-2 text-[13px]">
            {{ group.count }}
          </span>
        </button>

        @if (group.isOpen) {
          @for (row of group.rows; track row.change.id) {
            <div
              class="hover:bg-hover flex cursor-pointer items-start gap-2.5 border-l-2 py-3 pr-4 pl-3 transition-colors"
              [class.border-primary]="row.isSelected"
              [class.bg-primary-selected]="row.isSelected"
              [class.border-transparent]="!row.isSelected"
              [class.opacity-50]="
                (isPending() && !row.isIncluded) || row.change.undoneAt
              "
              [attr.aria-current]="row.isSelected"
              (click)="selected.emit(row.change.id)">
              @if (isPending()) {
                <span class="mt-0.5" (click)="stopPropagation($event)">
                  <app-selection-checkbox
                    [checked]="row.isIncluded"
                    [disabled]="!row.isSelectable"
                    [label]="row.change.summary"
                    (changed)="toggled.emit(row.change.id)" />
                </span>
              } @else {
                <span
                  class="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center">
                  @if (row.change.undoneAt) {
                    <svg lucideUndo2 class="text-muted h-5 w-5"></svg>
                  } @else {
                    @switch (row.change.applyStatus) {
                      @case (applyStatus.applied) {
                        <svg
                          lucideCircleCheck
                          class="text-change-added h-5 w-5"></svg>
                      }
                      @case (applyStatus.failed) {
                        <svg
                          lucideTriangleAlert
                          class="text-change-removed h-5 w-5"></svg>
                      }
                      @default {
                        <svg lucideMinus class="text-muted h-5 w-5"></svg>
                      }
                    }
                  }
                </span>
              }

              <span class="flex min-w-0 flex-1 flex-col gap-1">
                <span class="flex min-w-0 items-baseline gap-1.5">
                  <span
                    class="font-avatar shrink-0 text-sm font-bold"
                    [class.text-change-added]="row.letter === 'A'"
                    [class.text-change-modified]="row.letter === 'M'"
                    [class.text-change-removed]="row.letter === 'D'"
                    [attr.aria-hidden]="true">
                    {{ row.letter }}
                  </span>
                  <span
                    class="min-w-0 truncate text-[15px]"
                    [title]="row.title">
                    {{ row.title }}
                  </span>
                </span>
                <span class="text-muted truncate pl-[22px] text-[13px]">
                  {{ row.detail }}
                </span>
              </span>

              @if (row.isBlocked) {
                <svg
                  lucideTriangleAlert
                  class="text-change-removed mt-0.5 h-[17px] w-[17px] shrink-0"
                  i18n-aria-label="
                    Accessible label on the marker shown beside a change that
                    cannot be applied
                  "
                  aria-label="Cannot be applied"></svg>
              }
            </div>
          }
        }
      </div>
    } @empty {
      <p
        class="text-muted px-3 py-10 text-center text-sm"
        i18n="Shown when a change set has no changes left to review">
        There is nothing to review.
      </p>
    }
  `,
})
export class AiAssistantReviewListComponent {
  readonly groups = input.required<AiChangeGroup[]>();
  readonly excludedChangeIds = input.required<Set<number>>();
  readonly collapsedKeys = input.required<Set<string>>();
  readonly selectedChangeId = input<number | null>(null);
  readonly isPending = input(false);

  readonly selected = output<number>();
  readonly toggled = output<number>();
  readonly groupToggled = output<string>();

  protected readonly applyStatus = AiChangeApplyStatus;

  protected readonly reviewGroups = computed<AiReviewGroup[]>(() => {
    const excluded = this.excludedChangeIds();
    const collapsed = this.collapsedKeys();
    const selectedId = this.selectedChangeId();

    return this.groups().map((group) => {
      const rows = group.changes.map((change) => {
        const isSelectable = isValid(change);
        const summary = changeSummary(change);

        return {
          change,
          letter: changeLetter(change),
          title: summary.target ?? change.summary,
          detail: summary.target
            ? summary.detail
            : entityLabel(change.entityType),
          isSelectable,
          isIncluded: isSelectable && !excluded.has(change.id),
          isSelected: change.id === selectedId,
          isBlocked: !isSelectable,
        };
      });

      return {
        key: group.key,
        entity: group.label,
        title: changeSummary(group.changes[0]).target ?? group.label,
        count: rows.length,
        isOpen: !collapsed.has(group.key),
        rows,
      };
    });
  });

  protected stopPropagation(event: Event) {
    event.stopPropagation();
  }
}
