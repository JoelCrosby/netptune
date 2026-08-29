import { Component, computed, inject, input, output } from '@angular/core';
import {
  AiChangeApplyStatus,
  AiChangeSet,
  AiChangeSetStatus,
} from '@core/models/ai-conversation';
import { DialogService } from '@core/services/dialog.service';
import {
  LucideCheck,
  LucideLoaderCircle,
  LucideTriangleAlert,
} from '@lucide/angular';
import { SelectionCheckboxComponent } from '@static/components/checkbox/selection-checkbox.component';
import { groupChanges, isApplied, isValid } from './ai-assistant-change-group';
import {
  AiDigestRowView,
  AiInlineGroup,
  digestRows,
  inlineHeading,
  inlineRow,
} from './ai-assistant-change-summary';
import {
  AiAssistantReviewDialogComponent,
  AiReviewData,
} from './ai-assistant-review-dialog.component';

/** Past this, the rows stop being a summary and the digest takes over. */
const INLINE_ROW_LIMIT = 3;

@Component({
  selector: 'app-ai-assistant-change-set',
  host: { class: 'block' },
  imports: [
    LucideCheck,
    LucideLoaderCircle,
    LucideTriangleAlert,
    SelectionCheckboxComponent,
  ],
  template: `
    <div class="mx-auto w-full px-4 py-3" [class]="contentWidth()">
      <section
        class="border-border bg-card rise-in flex flex-col overflow-hidden rounded-[10px] border"
        [attr.aria-label]="blockLabel">
        @if (isPending()) {
          <div
            class="border-border bg-card-header flex items-center gap-2 border-b px-3 py-2.5">
            <h3
              class="text-[13px] font-medium whitespace-nowrap"
              i18n="Heading above the list of proposed workspace changes">
              Proposed changes
            </h3>
            <span
              class="bg-foreground/9 text-muted flex h-4.5 min-w-4.5 items-center justify-center rounded-full px-1.5 text-[11px] tabular-nums">
              {{ total() }}
            </span>
            <span class="flex-1"></span>
            <button
              type="button"
              class="text-primary text-xs hover:underline"
              (click)="reviewAll()"
              i18n="Button that opens the full screen review of the changes">
              Review all
            </button>
          </div>

          @if (isDigest()) {
            <div class="flex flex-col">
              @for (row of digest(); track row.key) {
                <div
                  class="border-border/55 flex items-center gap-2.25 border-b px-3 py-2.25 last:border-b-0">
                  <app-selection-checkbox
                    [checked]="row.isIncluded"
                    [label]="row.label"
                    (changed)="toggleRow(row)" />
                  <span
                    class="font-avatar w-3.5 shrink-0 text-[12px] font-bold"
                    [class]="letterColour(row.letter)"
                    aria-hidden="true">
                    {{ row.letter }}
                  </span>
                  <span
                    class="text-muted min-w-0 flex-1 truncate text-[13px]"
                    [title]="row.label"
                    >{{ row.lead
                    }}<span class="text-foreground">{{ row.emphasis }}</span
                    >{{ row.trail }}</span
                  >
                  @if (row.scope; as scope) {
                    <span class="text-muted shrink-0 text-[11.5px]">
                      {{ scope }}
                    </span>
                  }
                </div>
              }
            </div>
          } @else {
            @for (group of inlineGroups(); track group.key) {
              <div
                class="text-muted px-3 pt-2.25 pb-1 text-[11px] tracking-[.04em] uppercase">
                {{ group.heading }}
              </div>

              @for (row of group.rows; track row.change.id) {
                <div class="flex items-start gap-2.25 px-3 pt-1.75 pb-2.25">
                  <app-selection-checkbox
                    class="mt-0.5"
                    [checked]="row.isIncluded"
                    [disabled]="!row.isSelectable"
                    [label]="row.change.summary"
                    (changed)="toggled.emit(row.change.id)" />
                  <span
                    class="font-avatar mt-px w-3.5 shrink-0 text-[12px] font-bold"
                    [class]="letterColour(row.letter)"
                    aria-hidden="true">
                    {{ row.letter }}
                  </span>

                  <span class="flex min-w-0 flex-1 flex-col gap-0.75">
                    @for (field of row.fields; track field.key) {
                      @if (field.isProse) {
                        <span class="text-[13px]">{{ field.label }}</span>

                        @for (line of field.lines; track $index) {
                          <span
                            class="font-avatar truncate text-[11.5px] leading-normal"
                            [class]="
                              line.isAdded ? 'text-foreground/75' : 'text-muted'
                            ">
                            <span [class]="markColour(line.isAdded)">{{
                              line.mark
                            }}</span>
                            {{ line.text }}
                          </span>
                        }
                      } @else {
                        <span
                          class="flex min-w-0 items-baseline gap-1.75 text-[13px]">
                          <span class="shrink-0">{{ field.label }}</span>

                          @if (field.swap; as swap) {
                            <span
                              class="text-muted font-avatar truncate text-[11.5px] line-through">
                              {{ swap.before }}
                            </span>
                            <span
                              class="text-foreground/35 shrink-0 text-[11px]"
                              aria-hidden="true">
                              &rarr;
                            </span>
                            <span
                              class="text-change-added font-avatar truncate text-[11.5px]">
                              {{ swap.after }}
                            </span>
                          } @else if (field.single; as single) {
                            <span
                              class="font-avatar shrink-0 text-[11.5px]"
                              [class]="markColour(single.isAdded)">
                              {{ single.mark }}
                            </span>
                            <span
                              class="font-avatar truncate text-[11.5px]"
                              [class]="
                                single.isAdded
                                  ? 'text-foreground/75'
                                  : 'text-muted'
                              ">
                              {{ single.text }}
                            </span>
                          }
                        </span>
                      }
                    }
                  </span>
                </div>
              }
            }
          }

          @if (blockedCount() > 0) {
            <button
              type="button"
              class="border-border/55 hover:bg-card-hover flex w-full items-center gap-2.25 border-t px-3 py-2.25 text-left transition-colors"
              (click)="reviewBlocked()">
              <span class="h-4 w-4 shrink-0"></span>
              <span
                class="font-avatar text-change-removed w-3.5 shrink-0 text-[12px] font-bold"
                aria-hidden="true">
                !
              </span>
              <span class="text-muted min-w-0 flex-1 truncate text-[13px]"
                ><span class="text-foreground">{{ blockedLabel() }}</span>
                {{ blockedSuffix() }}</span
              >
            </button>
          }

          <div
            class="border-border bg-card-header flex items-center gap-2 border-t px-3 py-2.5">
            <button
              type="button"
              class="bg-primary text-primary-foreground hover:bg-primary/88 flex h-8 items-center justify-center gap-1.5 rounded-md px-3.5 text-[13px] font-medium whitespace-nowrap transition-colors disabled:opacity-50"
              [disabled]="isApplying() || selectedCount() === 0"
              [attr.aria-label]="applyLabel()"
              (click)="applied.emit()">
              @if (isApplying()) {
                <svg lucideLoaderCircle class="h-3.5 w-3.5 animate-spin"></svg>
              }
              <span aria-hidden="true">
                <span i18n="Button that applies the proposed changes"
                  >Apply</span
                >
                <span>&nbsp;({{ selectedCount() }})</span>
              </span>
            </button>
            <span class="flex-1"></span>
            <button
              type="button"
              class="text-muted hover:text-foreground/80 px-1 text-[12.5px] transition-colors"
              [disabled]="isApplying()"
              (click)="discarded.emit()"
              i18n="Button that discards the proposed changes">
              Discard
            </button>
          </div>
        } @else {
          <div class="rise-in flex items-center gap-2 px-3 py-2.5 text-[13px]">
            @if (isDiscarded()) {
              <span class="text-muted">{{ outcome() }}</span>
            } @else {
              @if (failedCount() > 0) {
                <svg
                  lucideTriangleAlert
                  class="text-change-removed h-3.5 w-3.5 shrink-0"></svg>
              } @else {
                <svg
                  lucideCheck
                  class="text-change-added h-3.5 w-3.5 shrink-0"></svg>
              }
              <span class="text-muted min-w-0 flex-1 truncate">
                {{ outcome() }}
              </span>

              @if (failedCount() > 0) {
                <button
                  type="button"
                  class="text-primary shrink-0 text-xs hover:underline"
                  (click)="reviewFailed()"
                  i18n="
                    Button that opens the review filtered to the changes that
                    could not be applied
                  ">
                  Review failed
                </button>
              }

              @if (canUndo()) {
                <button
                  type="button"
                  class="text-primary shrink-0 text-xs hover:underline"
                  [disabled]="isApplying()"
                  (click)="undone.emit()"
                  i18n="Button that takes back an applied change set">
                  Undo
                </button>
              }
            }
          </div>
        }
      </section>
    </div>
  `,
})
export class AiAssistantChangeSetComponent {
  readonly changeSet = input.required<AiChangeSet>();
  readonly excludedChangeIds = input.required<Set<number>>();
  readonly isApplying = input(false);
  readonly contentWidth = input('');

  readonly toggled = output<number>();
  readonly applied = output();
  readonly discarded = output();
  readonly undone = output();
  readonly selectionChanged = output<number[]>();

  private readonly dialog = inject(DialogService);

  protected readonly blockLabel = $localize`:Accessible label of the block holding the proposed changes:Proposed changes`;

  protected readonly changes = computed(() => this.changeSet().changes);

  protected readonly total = computed(() => this.changes().length);

  protected readonly isPending = computed(() => {
    return this.changeSet().status === AiChangeSetStatus.pending;
  });

  protected readonly isDiscarded = computed(() => {
    return this.changeSet().status === AiChangeSetStatus.discarded;
  });

  protected readonly isDigest = computed(() => {
    return this.total() > INLINE_ROW_LIMIT;
  });

  protected readonly digest = computed<AiDigestRowView[]>(() => {
    const excluded = this.excludedChangeIds();

    return digestRows(this.changes()).map((row) => {
      const isIncluded = row.changeIds.every((id) => !excluded.has(id));

      return { ...row, isIncluded };
    });
  });

  protected readonly inlineGroups = computed<AiInlineGroup[]>(() => {
    const excluded = this.excludedChangeIds();

    return groupChanges(this.changes()).map((group) => ({
      key: group.key,
      heading: inlineHeading(group.label, group.changes),
      rows: group.changes.map((change) => inlineRow(change, excluded)),
    }));
  });

  protected readonly selectable = computed(() => {
    return this.changes().filter(isValid);
  });

  protected readonly selectedCount = computed(() => {
    const excluded = this.excludedChangeIds();

    return this.selectable().filter((change) => !excluded.has(change.id))
      .length;
  });

  protected readonly blockedCount = computed(() => {
    return this.changes().length - this.selectable().length;
  });

  protected readonly blockedLabel = computed(() => {
    const blocked = this.blockedCount();

    if (blocked === 1) {
      return $localize`:One change cannot be applied:1 change`;
    }

    return $localize`:Number of changes that cannot be applied:${blocked}:BLOCKED: changes`;
  });

  protected readonly blockedSuffix = computed(() => {
    if (this.blockedCount() === 1) {
      return $localize`:one blocked change|Follows the count when a single change cannot be applied:cannot be applied`;
    }

    return $localize`:many blocked changes|Follows the number of changes that cannot be applied:cannot be applied`;
  });

  protected readonly applyLabel = computed(() => {
    const selected = this.selectedCount();

    return $localize`:Accessible label of the button that applies the selected changes:Apply ${selected}:SELECTED: changes`;
  });

  protected readonly failedCount = computed(() => {
    const changeSet = this.changeSet();
    const isUndone =
      changeSet.undoneAt !== null && changeSet.undoneAt !== undefined;

    if (isUndone) {
      return 0;
    }

    return this.changes().filter((change) => {
      return change.applyStatus === AiChangeApplyStatus.failed;
    }).length;
  });

  /** Only a change that actually landed, and knows how to reverse itself, can go back. */
  protected readonly canUndo = computed(() => {
    const changeSet = this.changeSet();
    const isUndone =
      changeSet.undoneAt !== null && changeSet.undoneAt !== undefined;

    if (this.isPending() || isUndone) {
      return false;
    }

    return this.changes().some((change) => {
      return isApplied(change) && change.canUndo && !change.undoneAt;
    });
  });

  protected readonly outcome = computed(() => {
    if (this.isDiscarded()) {
      return $localize`:Shown after proposals were discarded:These changes were discarded.`;
    }

    const changes = this.changes();
    const undoneCount = changes.filter((change) => change.undoneAt).length;

    if (undoneCount > 0) {
      return $localize`:Shown after applied proposals were taken back:${undoneCount}:UNDONE: of these changes were undone.`;
    }

    const total = changes.length;
    const applied = changes.filter(isApplied).length;
    const failed = this.failedCount();

    if (failed > 0) {
      return $localize`:Shown after a change set was partly applied:${applied}:APPLIED: of ${total}:TOTAL: applied · ${failed}:FAILED: failed`;
    }

    if (applied === 1) {
      return $localize`:Shown after a single change was applied:1 change applied`;
    }

    return $localize`:Shown after changes were applied:${applied}:APPLIED: changes applied`;
  });

  protected letterColour(letter: string | null): string {
    if (letter === 'A') {
      return 'text-change-added';
    }

    if (letter === 'D') {
      return 'text-change-removed';
    }

    return 'text-change-modified';
  }

  protected markColour(isAdded: boolean): string {
    return isAdded ? 'text-change-added' : 'text-change-removed';
  }

  protected toggleRow(row: AiDigestRowView) {
    const excluded = this.excludedChangeIds();
    const changed = row.changeIds.filter((id) => {
      return excluded.has(id) !== row.isIncluded;
    });

    this.selectionChanged.emit(changed);
  }

  protected reviewAll() {
    this.openReview();
  }

  protected reviewBlocked() {
    this.openReview('blocked');
  }

  protected reviewFailed() {
    this.openReview('failed');
  }

  private openReview(filter?: AiReviewData['filter']) {
    this.dialog.open<unknown, AiReviewData>(AiAssistantReviewDialogComponent, {
      data: { filter },
      width: '100vw',
      maxWidth: '100vw',
      height: '100vh',
      panelClass: 'np-review-dialog',
    });
  }
}
