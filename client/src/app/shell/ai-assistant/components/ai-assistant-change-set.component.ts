import { Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  AiChangeApplyStatus,
  AiChangeSet,
  AiChangeSetStatus,
  AiProposedChange,
} from '@core/models/ai-conversation';
import { referenceRoute } from '@core/util/ai-references';
import {
  LucideCalendarRange,
  LucideCircleAlert,
  LucideCircleCheck,
  LucideCircleDashed,
  LucideFolder,
  LucideKanban,
  LucideSquareCheckBig,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  AiChangeGroup,
  fieldLabel,
  groupChanges,
  isApplied,
  isValid,
} from './ai-assistant-change-group';

@Component({
  selector: 'app-ai-assistant-change-set',
  host: { class: 'border-border block border-t' },
  imports: [
    FlatButtonComponent,
    StrokedButtonComponent,
    RouterLink,
    LucideCalendarRange,
    LucideCircleAlert,
    LucideCircleCheck,
    LucideCircleDashed,
    LucideFolder,
    LucideKanban,
    LucideSquareCheckBig,
  ],
  template: `
    <div class="mx-auto w-full px-4 py-3" [class]="contentWidth()">
      <div class="mb-2 flex items-center justify-between gap-2">
        <h3
          class="font-overpass text-sm font-medium"
          i18n="Heading above the list of proposed workspace changes">
          Proposed changes
        </h3>

        @if (isPending() && selectableCount() > 1) {
          <button
            type="button"
            class="text-muted hover:text-foreground text-xs"
            (click)="toggleAll()">
            @if (isEveryChangeSelected()) {
              <span i18n="Button that clears every selected change">
                Select none
              </span>
            } @else {
              <span i18n="Button that selects every change">Select all</span>
            }
          </button>
        }
      </div>

      <div class="flex flex-col gap-3">
        @for (group of groups(); track group.key) {
          <div class="border-border rounded-lg border">
            <div
              class="text-muted border-border flex items-center gap-1.5 border-b px-2 py-1 text-xs">
              @switch (group.entityType) {
                @case ('task') {
                  <svg lucideSquareCheckBig class="h-3 w-3"></svg>
                }
                @case ('project') {
                  <svg lucideFolder class="h-3 w-3"></svg>
                }
                @case ('sprint') {
                  <svg lucideCalendarRange class="h-3 w-3"></svg>
                }
                @case ('board') {
                  <svg lucideKanban class="h-3 w-3"></svg>
                }
                @default {
                  <svg lucideCircleDashed class="h-3 w-3"></svg>
                }
              }
              <span>{{ group.label }}</span>
            </div>

            <div class="flex flex-col gap-2 p-2">
              @for (change of group.changes; track change.id) {
                <div class="flex items-start gap-2 text-sm">
                  @if (isPending()) {
                    <input
                      type="checkbox"
                      class="mt-1"
                      [checked]="isIncluded(change)"
                      [disabled]="!canSelect(change)"
                      (change)="toggled.emit(change.id)" />
                  } @else {
                    <span class="mt-0.5 flex h-4 w-4 items-center justify-center">
                      @if (wasApplied(change)) {
                        <svg lucideCircleCheck class="text-primary h-3.5 w-3.5"></svg>
                      } @else {
                        <svg lucideCircleAlert class="text-muted h-3.5 w-3.5"></svg>
                      }
                    </span>
                  }

                  <span class="flex min-w-0 flex-col gap-0.5">
                    @if (routeFor(change); as route) {
                      <a [routerLink]="route" class="hover:underline">{{
                        change.summary
                      }}</a>
                    } @else {
                      <span>{{ change.summary }}</span>
                    }

                    @for (field of change.fields; track field.name) {
                      <span class="text-muted flex flex-wrap items-center gap-1 text-xs">
                        <span class="font-medium">{{ label(field.name) }}</span>
                        @if (field.before) {
                          <span class="line-through">{{ field.before }}</span>
                          <span aria-hidden="true">→</span>
                        }
                        @if (field.after) {
                          <span>{{ field.after }}</span>
                        } @else {
                          <span class="italic" i18n="Shown when a change removes a value">
                            cleared
                          </span>
                        }
                      </span>
                    }

                    @if (!canSelect(change)) {
                      <span class="text-muted text-xs italic">
                        {{ validationMessage(change) }}
                      </span>
                    }

                    @if (change.applyError) {
                      <span class="text-error text-xs">{{ change.applyError }}</span>
                    }
                  </span>
                </div>
              }
            </div>
          </div>
        }
      </div>

      @if (isPending()) {
        <div class="mt-3 flex items-center gap-2">
          <button
            app-flat-button
            type="button"
            [disabled]="isApplying() || selectedCount() === 0"
            (click)="applied.emit()">
            <span i18n="Button that applies the proposed changes">Apply</span>
            <span>&nbsp;({{ selectedCount() }})</span>
          </button>
          <button app-stroked-button type="button" (click)="discarded.emit()">
            <span i18n="Button that discards the proposed changes">Discard</span>
          </button>
        </div>
      } @else {
        <p class="text-muted mt-3 text-xs">{{ outcome() }}</p>
      }
    </div>
  `,
})
export class AiAssistantChangeSetComponent {
  readonly changeSet = input.required<AiChangeSet>();
  readonly excludedChangeIds = input.required<Set<number>>();
  readonly isApplying = input(false);
  readonly contentWidth = input('');
  readonly workspace = input<string | null>(null);

  readonly toggled = output<number>();
  readonly applied = output();
  readonly discarded = output();
  readonly selectionChanged = output<number[]>();

  protected readonly label = fieldLabel;

  protected readonly isPending = computed(() => {
    return this.changeSet().status === AiChangeSetStatus.pending;
  });

  protected readonly groups = computed<AiChangeGroup[]>(() => {
    return groupChanges(this.changeSet().changes);
  });

  protected readonly selectable = computed(() => {
    return this.changeSet().changes.filter(isValid);
  });

  protected readonly selectableCount = computed(() => this.selectable().length);

  protected readonly selectedCount = computed(() => {
    const excluded = this.excludedChangeIds();

    return this.selectable().filter((change) => !excluded.has(change.id)).length;
  });

  protected readonly isEveryChangeSelected = computed(() => {
    return this.selectedCount() === this.selectableCount();
  });

  protected readonly outcome = computed(() => {
    const changes = this.changeSet().changes;
    const applied = changes.filter(isApplied).length;
    const failed = changes.filter((change) => {
      return change.applyStatus === AiChangeApplyStatus.failed;
    }).length;

    if (failed > 0) {
      return $localize`:Shown after a change set was partly applied:${applied}:APPLIED: of ${changes.length}:TOTAL: changes were applied. ${failed}:FAILED: failed.`;
    }

    const skipped = changes.length - applied;

    if (skipped > 0) {
      return $localize`:Shown after some changes were left out:${applied}:APPLIED: of ${changes.length}:TOTAL: changes were applied.`;
    }

    return $localize`:Shown after changes were applied:These changes have been applied.`;
  });

  protected isIncluded(change: AiProposedChange): boolean {
    return this.canSelect(change) && !this.excludedChangeIds().has(change.id);
  }

  protected canSelect(change: AiProposedChange): boolean {
    return isValid(change);
  }

  protected wasApplied(change: AiProposedChange): boolean {
    return isApplied(change);
  }

  protected validationMessage(change: AiProposedChange): string {
    const message = change.validationMessage;

    if (message) {
      return message;
    }

    return $localize`:Shown on a proposal that cannot be applied:This change cannot be applied.`;
  }

  protected routeFor(change: AiProposedChange): string[] | null {
    const workspace = this.workspace();
    const canLink = workspace !== null && isApplied(change);

    if (!canLink) {
      return null;
    }

    const identifier = change.entitySystemId ?? change.appliedEntityId ?? change.entityId;

    if (!identifier) {
      return null;
    }

    return referenceRoute(workspace, change.entityType, `${identifier}`);
  }

  protected toggleAll() {
    const excluded = this.excludedChangeIds();
    const shouldClear = this.isEveryChangeSelected();
    const changed = this.selectable()
      .filter((change) => excluded.has(change.id) !== shouldClear)
      .map((change) => change.id);

    this.selectionChanged.emit(changed);
  }
}
