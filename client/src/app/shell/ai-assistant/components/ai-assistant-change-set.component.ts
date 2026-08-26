import {
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import {
  AiChangeApplyStatus,
  AiChangeSet,
  AiChangeSetStatus,
} from '@core/models/ai-conversation';
import { DialogService } from '@core/services/dialog.service';
import { LucideChevronDown } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  AiChangeGroup,
  groupChanges,
  isApplied,
  isValid,
} from './ai-assistant-change-group';
import { AiAssistantChangeListComponent } from './ai-assistant-change-list.component';
import { AiAssistantReviewDialogComponent } from './ai-assistant-review-dialog.component';

/** Enough to read at a glance without pushing the composer off the screen. */
const INLINE_CHANGE_LIMIT = 3;

@Component({
  selector: 'app-ai-assistant-change-set',
  host: { class: 'border-border block border-t' },
  imports: [
    LucideChevronDown,
    FlatButtonComponent,
    StrokedButtonComponent,
    AiAssistantChangeListComponent,
  ],
  template: `
    <div class="mx-auto w-full px-4 py-3" [class]="contentWidth()">
      <div class="flex items-center justify-between gap-2 px-1 py-1">
        <button
          type="button"
          class="flex min-w-0 items-center gap-1.5 text-left"
          [attr.aria-expanded]="!isCollapsed()"
          (click)="toggleCollapsed()">
          <svg
            lucideChevronDown
            class="text-muted h-3.5 w-3.5 shrink-0 transition-transform"
            [class.-rotate-90]="isCollapsed()"></svg>
          <h3
            class="font-overpass text-sm font-medium"
            i18n="Heading above the list of proposed workspace changes">
            Proposed changes
          </h3>
          @if (isCollapsed()) {
            <span class="text-muted truncate text-xs">{{
              collapsedLabel()
            }}</span>
          }
        </button>

        @if (!isCollapsed()) {
          <div class="flex shrink-0 items-center gap-3">
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
                  <span i18n="Button that selects every change"
                    >Select all</span
                  >
                }
              </button>
            }

            <button
              type="button"
              class="text-muted hover:text-foreground text-xs"
              (click)="showAll()"
              i18n="Button that opens the detailed table of proposed changes">
              Review all
            </button>
          </div>
        }
      </div>

      @if (!isCollapsed()) {
        <app-ai-assistant-change-list
          [groups]="visibleGroups()"
          [excludedChangeIds]="excludedChangeIds()"
          [isPending]="isPending()"
          [workspace]="workspace()"
          (toggled)="toggled.emit($event)" />

        @if (hiddenCount() > 0) {
          <div class="mt-2 flex justify-center">
            <button
              type="button"
              class="bg-hover text-muted hover:text-foreground rounded-full px-4 py-2 text-xs transition-colors"
              (click)="showAll()">
              {{ moreLabel() }}
            </button>
          </div>
        }

        @if (isPending()) {
          <div class="mt-2 flex items-center gap-2 px-1">
            <button
              app-flat-button
              type="button"
              class="flex-1 rounded-full"
              [disabled]="isApplying() || selectedCount() === 0"
              (click)="applied.emit()">
              <span i18n="Button that applies the proposed changes">Apply</span>
              <span>&nbsp;({{ selectedCount() }})</span>
            </button>
            <button
              app-stroked-button
              type="button"
              class="flex-1 rounded-full"
              (click)="discarded.emit()">
              <span i18n="Button that discards the proposed changes"
                >Discard</span
              >
            </button>
          </div>
        } @else {
          <div class="mt-2 flex items-center justify-between gap-2 px-1">
            <p class="text-muted text-xs">{{ outcome() }}</p>

            <span class="flex shrink-0 items-center gap-3">
              @if (failedCount() > 0) {
                <button
                  type="button"
                  class="text-muted hover:text-foreground text-xs"
                  [disabled]="isApplying()"
                  (click)="retried.emit()">
                  <span i18n="Button that runs the changes that failed again">
                    Retry failed
                  </span>
                  <span>&nbsp;({{ failedCount() }})</span>
                </button>
              }

              @if (canUndo()) {
                <button
                  type="button"
                  class="text-muted hover:text-foreground text-xs"
                  [disabled]="isApplying()"
                  (click)="undone.emit()">
                  <span i18n="Button that takes back an applied change set">
                    Undo
                  </span>
                </button>
              }
            </span>
          </div>
        }
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
  readonly undone = output();
  readonly retried = output();
  readonly selectionChanged = output<number[]>();

  private readonly dialog = inject(DialogService);

  protected readonly isPending = computed(() => {
    return this.changeSet().status === AiChangeSetStatus.pending;
  });

  /** Only a change that actually landed, and knows how to reverse itself, can go back. */
  protected readonly canUndo = computed(() => {
    const changeSet = this.changeSet();
    const isUndone =
      changeSet.undoneAt !== null && changeSet.undoneAt !== undefined;

    if (this.isPending() || isUndone) {
      return false;
    }

    return changeSet.changes.some((change) => {
      return isApplied(change) && change.canUndo && !change.undoneAt;
    });
  });

  protected readonly failedCount = computed(() => {
    const changeSet = this.changeSet();
    const isUndone =
      changeSet.undoneAt !== null && changeSet.undoneAt !== undefined;

    if (isUndone) {
      return 0;
    }

    return changeSet.changes.filter((change) => {
      return change.applyStatus === AiChangeApplyStatus.failed;
    }).length;
  });

  /** null follows the change set: open while it needs a decision, closed once it has one. */
  private readonly collapsePreference = signal<boolean | null>(null);

  private trackedState: string | null = null;

  protected readonly isCollapsed = computed(() => {
    return this.collapsePreference() ?? !this.isPending();
  });

  protected readonly groups = computed<AiChangeGroup[]>(() => {
    return groupChanges(this.changeSet().changes);
  });

  /** Groups are kept whole, so the cap is a floor on what is shown, not a hard count. */
  protected readonly visibleGroups = computed<AiChangeGroup[]>(() => {
    const visible: AiChangeGroup[] = [];
    let shown = 0;

    for (const group of this.groups()) {
      if (shown >= INLINE_CHANGE_LIMIT) {
        break;
      }

      visible.push(group);
      shown += group.changes.length;
    }

    return visible;
  });

  protected readonly hiddenCount = computed(() => {
    const shown = this.visibleGroups().reduce((total, group) => {
      return total + group.changes.length;
    }, 0);

    return this.changeSet().changes.length - shown;
  });

  protected readonly moreLabel = computed(() => {
    const hidden = this.hiddenCount();

    return $localize`:Opens a dialog with the changes not shown inline:+${hidden}:HIDDEN: more changes`;
  });

  constructor() {
    effect(() => {
      const state = `${this.changeSet().id}:${this.isPending()}`;
      const hasChanged = state !== this.trackedState;

      if (!hasChanged) {
        return;
      }

      this.trackedState = state;
      this.collapsePreference.set(null);
    });
  }

  protected readonly selectable = computed(() => {
    return this.changeSet().changes.filter(isValid);
  });

  protected readonly selectableCount = computed(() => this.selectable().length);

  protected readonly selectedCount = computed(() => {
    const excluded = this.excludedChangeIds();

    return this.selectable().filter((change) => !excluded.has(change.id))
      .length;
  });

  protected readonly isEveryChangeSelected = computed(() => {
    return this.selectedCount() === this.selectableCount();
  });

  protected readonly collapsedLabel = computed(() => {
    const total = this.changeSet().changes.length;

    if (this.isPending()) {
      return $localize`:Summarises a collapsed set of proposals awaiting a decision:${total}:TOTAL: awaiting review`;
    }

    return this.outcome();
  });

  protected readonly outcome = computed(() => {
    const isDiscarded = this.changeSet().status === AiChangeSetStatus.discarded;

    if (isDiscarded) {
      return $localize`:Shown after proposals were discarded:These changes were discarded.`;
    }

    const undoneCount = this.changeSet().changes.filter((change) => {
      return change.undoneAt;
    }).length;

    if (undoneCount > 0) {
      return $localize`:Shown after applied proposals were taken back:${undoneCount}:UNDONE: of these changes were undone.`;
    }

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

  protected toggleCollapsed() {
    this.collapsePreference.set(!this.isCollapsed());
  }

  protected showAll() {
    this.dialog.open(AiAssistantReviewDialogComponent, {
      width: '100vw',
      maxWidth: '100vw',
      height: '100vh',
      panelClass: 'np-review-dialog',
    });
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
