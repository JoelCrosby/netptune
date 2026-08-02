import { Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  AiChangeApplyStatus,
  AiProposedChange,
} from '@core/models/ai-conversation';
import {
  LucideCircleAlert,
  LucideCircleCheck,
  LucideMinus,
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
  TableRowDirective,
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
  message: string | null;
}

@Component({
  selector: 'app-ai-assistant-changes-table',
  imports: [
    RouterLink,
    BadgeComponent,
    LucideCircleAlert,
    LucideCircleCheck,
    LucideMinus,
    SelectionCheckboxComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeadDirective,
    TableHeaderRowDirective,
    TableRowDirective,
    AiAssistantChangeDetailComponent,
  ],
  template: `
    <app-table tableClass="table-fixed">
      <colgroup>
        <col class="w-10" />
        <col class="w-24" />
        <col class="w-28" />
        <col class="w-64" />
        <col class="w-40" />
        <col />
      </colgroup>

      <thead appTableHead [sticky]="true">
        <tr appTableHeaderRow class="bg-background">
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
          <th class="px-3 py-3">
            <span i18n="Column heading for the detail of a proposed change">
              Details
            </span>
          </th>
        </tr>
      </thead>

      <tbody>
        @for (row of rows(); track row.change.id) {
          <tr appTableRow [class.opacity-60]="!row.isIncluded && isPending()">
            <td class="px-3 py-3 align-top">
              @if (isPending()) {
                <app-selection-checkbox
                  class="mt-0.5 block"
                  [checked]="row.isIncluded"
                  [disabled]="!row.isSelectable"
                  [label]="row.change.summary"
                  (changed)="toggled.emit(row.change.id)" />
              } @else {
                <span class="mt-0.5 flex h-4 w-4 items-center justify-center">
                  @switch (row.change.applyStatus) {
                    @case (applyStatus.applied) {
                      <svg lucideCircleCheck class="text-primary h-4 w-4"></svg>
                    }
                    @case (applyStatus.failed) {
                      <svg lucideCircleAlert class="text-warn h-4 w-4"></svg>
                    }
                    @default {
                      <svg lucideMinus class="text-muted h-4 w-4"></svg>
                    }
                  }
                </span>
              }
            </td>

            <td class="px-3 py-3 align-top">
              <app-badge [color]="row.tone">{{ row.action }}</app-badge>
            </td>

            <td class="text-muted px-3 py-3 align-top text-xs">
              {{ row.entity }}
            </td>

            <td class="px-3 py-3 align-top">
              @if (row.target) {
                @if (row.route; as route) {
                  <a [routerLink]="route" class="break-words hover:underline">
                    {{ row.target }}
                  </a>
                } @else {
                  <span class="break-words">{{ row.target }}</span>
                }
              } @else {
                <span class="text-muted" aria-hidden="true">—</span>
              }
            </td>

            <td class="text-muted px-3 py-3 align-top text-xs">
              {{ row.detail }}
            </td>

            <td class="px-3 py-3 align-top">
              <app-ai-assistant-change-detail [change]="row.change" />

              @if (row.message) {
                <p class="text-warn mt-1 text-xs break-words">
                  {{ row.message }}
                </p>
              }
            </td>
          </tr>
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

  protected readonly rows = computed<AiChangeRow[]>(() => {
    const excluded = this.excludedChangeIds();
    const workspace = this.workspace();

    return this.changes().map((change) => {
      const isSelectable = isValid(change);
      const summary = changeSummary(change);

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
        message: this.message(change),
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
