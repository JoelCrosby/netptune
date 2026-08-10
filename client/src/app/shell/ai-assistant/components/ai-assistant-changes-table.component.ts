import { Component, computed, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  AiChangeApplyStatus,
  AiProposedChange,
} from '@core/models/ai-conversation';
import {
  LucideChevronRight,
  LucideCircleAlert,
  LucideCircleCheck,
  LucideMinus,
  LucideUndo2,
} from '@lucide/angular';
import {
  BadgeColor,
  BadgeComponent,
} from '@static/components/badge/badge.component';
import { SelectionCheckboxComponent } from '@static/components/checkbox/selection-checkbox.component';
import {
  TableComponent,
  TableEmptyCellDirective,
  TableHeadDirective,
  TableHeaderRowDirective,
} from '@static/components/table/table.component';
import { changeRoute, entityLabel, isValid } from './ai-assistant-change-group';
import {
  changeAction,
  changeSummary,
  changeTone,
} from './ai-assistant-change-kind';
import { AiAssistantChangeDetailComponent } from './ai-assistant-change-detail.component';

interface AiChangeRow {
  change: AiProposedChange;
  action: string;
  tone: BadgeColor;
  entity: string;
  target: string | null;
  detail: string;
  route: string[] | null;
  isSelectable: boolean;
  isIncluded: boolean;
  hasDetails: boolean;
  message: string | null;
}

@Component({
  selector: 'app-ai-assistant-changes-table',
  imports: [
    RouterLink,
    BadgeComponent,
    LucideChevronRight,
    LucideCircleAlert,
    LucideCircleCheck,
    LucideMinus,
    LucideUndo2,
    SelectionCheckboxComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeadDirective,
    TableHeaderRowDirective,
    AiAssistantChangeDetailComponent,
  ],
  template: `
    <app-table tableClass="table-fixed">
      <colgroup>
        <col class="w-9" />
        <col class="w-10" />
        <col class="w-24" />
        <col class="w-28" />
        <col class="w-64" />
        <col />
      </colgroup>

      <thead appTableHead>
        <tr appTableHeaderRow>
          <th class="py-3 pl-3">
            <span
              class="sr-only"
              i18n="Screen-reader heading for the column that expands a change">
              Details
            </span>
          </th>
          <th class="px-3 py-3">
            @if (isPending()) {
              <app-selection-checkbox
                [checked]="isEverythingIncluded()"
                [disabled]="selectableCount() === 0"
                i18n-label="
                  Accessible label for the checkbox that selects every proposed
                  change
                "
                label="Select every change"
                (changed)="toggledAll.emit()" />
            } @else {
              <span
                class="sr-only"
                i18n="Screen-reader heading for the change outcome column">
                Outcome
              </span>
            }
          </th>
          <th class="px-3 py-3">
            <span i18n="Column heading for what a proposed change does">
              Action
            </span>
          </th>
          <th class="px-3 py-3">
            <span i18n="Column heading for the kind of entity a change targets">
              Type
            </span>
          </th>
          <th class="px-3 py-3">
            <span i18n="Column heading for what a proposed change targets">
              Target
            </span>
          </th>
          <th class="px-3 py-3">
            <span i18n="Column heading for the sentence describing a change">
              Change
            </span>
          </th>
        </tr>
      </thead>

      <tbody>
        @for (row of rows(); track row.change.id) {
          <tr
            class="bg-card border-border hover:bg-card-hover transition-colors last:border-b-0"
            [class.border-b]="!isExpanded(row)"
            [class.cursor-pointer]="row.hasDetails"
            [class.opacity-60]="
              (!row.isIncluded && isPending()) || row.change.undoneAt
            "
            (click)="toggle(row)">
            <td class="py-3 pl-3 align-middle">
              @if (row.hasDetails) {
                <button
                  type="button"
                  class="text-muted hover:text-foreground -ml-1 rounded p-1"
                  [attr.aria-expanded]="isExpanded(row)"
                  i18n-aria-label="
                    Accessible label for the toggle that reveals what a proposed
                    change contains
                  "
                  aria-label="Change details"
                  (click)="toggle(row, $event)">
                  <svg
                    lucideChevronRight
                    class="h-4 w-4 transition-transform"
                    [class.rotate-90]="isExpanded(row)"></svg>
                </button>
              }
            </td>

            <td
              class="px-3 py-3 align-middle"
              (click)="stopPropagation($event)">
              @if (isPending()) {
                <app-selection-checkbox
                  class="block"
                  [checked]="row.isIncluded"
                  [disabled]="!row.isSelectable"
                  [label]="row.change.summary"
                  (changed)="toggled.emit(row.change.id)" />
              } @else {
                <span class="flex h-4 w-4 items-center justify-center">
                  @if (row.change.undoneAt) {
                    <svg lucideUndo2 class="text-muted h-4 w-4"></svg>
                  } @else {
                    @switch (row.change.applyStatus) {
                      @case (applyStatus.applied) {
                        <svg
                          lucideCircleCheck
                          class="text-primary h-4 w-4"></svg>
                      }
                      @case (applyStatus.failed) {
                        <svg lucideCircleAlert class="text-warn h-4 w-4"></svg>
                      }
                      @default {
                        <svg lucideMinus class="text-muted h-4 w-4"></svg>
                      }
                    }
                  }
                </span>
              }
            </td>

            <td class="px-3 py-3 align-middle">
              <app-badge [color]="row.tone">{{ row.action }}</app-badge>
            </td>

            <td class="text-muted px-3 py-3 align-middle text-xs">
              <span class="block truncate">{{ row.entity }}</span>
            </td>

            <td class="px-3 py-3 align-middle">
              @if (row.target) {
                @if (row.route; as route) {
                  <a
                    [routerLink]="route"
                    class="block truncate hover:underline"
                    [title]="row.target"
                    (click)="stopPropagation($event)">
                    {{ row.target }}
                  </a>
                } @else {
                  <span class="block truncate" [title]="row.target">
                    {{ row.target }}
                  </span>
                }
              } @else {
                <span class="text-muted" aria-hidden="true">—</span>
              }
            </td>

            <td class="px-3 py-3 align-middle">
              <div class="flex min-w-0 items-center gap-2 text-xs">
                <span class="text-muted truncate" [title]="row.detail">
                  {{ row.detail }}
                </span>

                @if (row.message) {
                  <span class="text-warn truncate" [title]="row.message">
                    {{ row.message }}
                  </span>
                }
              </div>
            </td>
          </tr>

          @if (isExpanded(row)) {
            <tr
              class="bg-card border-border border-b last:border-b-0"
              [class.opacity-60]="
                (!row.isIncluded && isPending()) || row.change.undoneAt
              ">
              <td colspan="2"></td>
              <td colspan="4" class="px-3 pt-0 pb-3">
                @if (row.message) {
                  <p class="text-warn mb-2 text-xs break-words">
                    {{ row.message }}
                  </p>
                }

                <app-ai-assistant-change-detail [change]="row.change" />
              </td>
            </tr>
          }
        } @empty {
          <tr>
            <td
              appTableEmptyCell
              colspan="6"
              i18n="Shown when a change set has no changes left to review">
              There is nothing to review.
            </td>
          </tr>
        }
      </tbody>
    </app-table>
  `,
})
export class AiAssistantChangesTableComponent {
  readonly changes = input.required<AiProposedChange[]>();
  readonly excludedChangeIds = input.required<Set<number>>();
  readonly isPending = input(false);
  readonly workspace = input<string | null>(null);

  readonly toggled = output<number>();
  readonly toggledAll = output();

  protected readonly applyStatus = AiChangeApplyStatus;

  private readonly expandedChangeIds = signal<Set<number>>(new Set());

  protected readonly rows = computed<AiChangeRow[]>(() => {
    const excluded = this.excludedChangeIds();
    const workspace = this.workspace();

    return this.changes().map((change) => {
      const isSelectable = isValid(change);
      const summary = changeSummary(change);
      const message = this.message(change);

      return {
        change,
        action: changeAction(change),
        tone: changeTone(change),
        entity: entityLabel(change.entityType),
        target: summary.target,
        detail: summary.detail,
        route: changeRoute(change, workspace),
        isSelectable,
        isIncluded: isSelectable && !excluded.has(change.id),
        hasDetails: change.fields.length > 0 || message !== null,
        message,
      };
    });
  });

  protected readonly selectableCount = computed(() => {
    return this.changes().filter(isValid).length;
  });

  protected readonly isEverythingIncluded = computed(() => {
    const included = this.rows().filter((row) => row.isIncluded).length;
    const selectable = this.selectableCount();

    return selectable > 0 && included === selectable;
  });

  protected isExpanded(row: AiChangeRow): boolean {
    return this.expandedChangeIds().has(row.change.id);
  }

  protected stopPropagation(event: Event) {
    event.stopPropagation();
  }

  protected toggle(row: AiChangeRow, event?: Event) {
    event?.stopPropagation();

    if (!row.hasDetails) {
      return;
    }

    this.expandedChangeIds.update((current) => {
      const next = new Set(current);

      if (next.has(row.change.id)) {
        next.delete(row.change.id);
      } else {
        next.add(row.change.id);
      }

      return next;
    });
  }

  private message(change: AiProposedChange): string | null {
    if (change.applyError) {
      return change.applyError;
    }

    const isBlocked = !isValid(change);

    if (!isBlocked) {
      return null;
    }

    const message = change.validationMessage;

    if (message) {
      return message;
    }

    return $localize`:Shown on a proposal that cannot be applied:This change cannot be applied.`;
  }
}
