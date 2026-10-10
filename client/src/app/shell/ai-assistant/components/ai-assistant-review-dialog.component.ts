import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  Component,
  computed,
  effect,
  forwardRef,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import {
  AiChangeApplyStatus,
  AiChangeSet,
  AiChangeSetStatus,
  AiProposedChange,
} from '@core/models/ai-conversation';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiPanelService } from '@core/services/ai-panel.service';
import { LucideSparkles, LucideX } from '@lucide/angular';
import { ButtonComponent } from '@static/components/button/button.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';
import { KeyboardKeyComponent } from '@static/components/keyboard-key/keyboard-key.component';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import {
  AiChangeGroup,
  batchGroups,
  groupChanges,
  isApplied,
  isTextField,
  isValid,
  selectableIds,
} from './ai-assistant-change-group';
import { changeSummary } from './ai-assistant-change-kind';
import { AiDiffMode, changeLetter } from './ai-assistant-diff';
import {
  AiAssistantReviewDetailComponent,
  AiFieldEdit,
  AiFieldTarget,
} from './ai-assistant-review-detail.component';
import { AiAssistantReviewListComponent } from './ai-assistant-review-list.component';
import { AiAssistantResizeHandleComponent } from './ai-assistant-resize-handle.component';
import { SpinnerIconComponent } from '@static/components/spinner/spinner-icon.component';
import { toggleInSet } from '@core/util/signals';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { AiAssistantPanelComponent } from '../ai-assistant-panel.component';

export type AiReviewFilter =
  'all' | 'created' | 'updated' | 'removed' | 'blocked' | 'failed';

/**
 * Opening the review without a change set reviews the conversation's live one.
 * A change set handed in is a record of what already happened, so the surface
 * reads it back without offering a decision.
 */
export interface AiReviewData {
  changeSet?: AiChangeSet;
  workspace?: string | null;
  filter?: AiReviewFilter;
}

const MODE_KEY = 'netptune.ai.review.mode';
const LIST_WIDTH_KEY = 'netptune.ai.review.list-width';

const MIN_LIST_WIDTH = 260;
const MAX_LIST_WIDTH = 720;
const DEFAULT_LIST_WIDTH = 440;

const clampListWidth = (width: number): number => {
  if (!Number.isFinite(width)) {
    return DEFAULT_LIST_WIDTH;
  }

  return Math.round(Math.min(MAX_LIST_WIDTH, Math.max(MIN_LIST_WIDTH, width)));
};

const pathOf = (url: string): string => {
  return url.split(/[?#]/)[0];
};

@Component({
  selector: 'app-ai-assistant-review-dialog',
  host: {
    class: 'flex h-full min-h-0 flex-col text-sm',
    '(document:keydown)': 'onKeydown($event)',
  },
  imports: [
    SpinnerIconComponent,
    LucideSparkles,
    LucideX,
    ButtonComponent,
    FlatButtonComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    EmptyStateComponent,
    FilterInputComponent,
    KeyboardKeyComponent,
    ProgressBarComponent,
    SegmentedControlComponent,
    AiAssistantReviewDetailComponent,
    AiAssistantReviewListComponent,
    AiAssistantResizeHandleComponent,
    TooltipDirective,
    forwardRef(() => AiAssistantPanelComponent),
  ],
  template: `
    <header
      class="border-border bg-card-header flex items-center gap-4 border-b py-3 pr-2.5 pl-4">
      <div class="flex min-w-0 flex-1 items-baseline gap-2.5">
        <h1 class="font-overpass m-0 text-[22px] font-medium whitespace-nowrap">
          {{ title() }}
        </h1>
        <span class="text-muted truncate text-[15px]">{{
          conversationTitle()
        }}</span>
      </div>

      @if (isRunning()) {
        <span
          class="bg-primary/12 text-primary flex shrink-0 items-center gap-2 rounded-full px-3.5 py-1.5 text-[13px] font-medium">
          <app-spinner-icon class="h-3.5 w-3.5 text-current" />
          @if (isStoppingApply()) {
            <span i18n="Shown on the review while a run is being stopped"
              >Stopping</span
            >
          } @else {
            <span i18n="Shown on the review while changes are being applied"
              >Applying</span
            >
          }
        </span>
      }

      @if (canChat) {
        <button
          app-icon-button
          class="h-10 w-10"
          type="button"
          [class.text-primary]="isAssistantOpen()"
          [attr.aria-pressed]="isAssistantOpen()"
          i18n-appTooltip="
            Tooltip on the button that shows or hides the assistant beside the
            review
          "
          appTooltip="Assistant"
          appTooltipPosition="bottom"
          i18n-aria-label="
            Accessible label for the button that shows or hides the assistant
            beside the review
          "
          aria-label="Assistant"
          (click)="isAssistantOpen.set(!isAssistantOpen())">
          <svg lucideSparkles class="h-5 w-5"></svg>
        </button>
      }

      <button
        app-icon-button
        class="h-10 w-10"
        type="button"
        i18n-aria-label="Accessible label for the button that closes the review"
        aria-label="Close review"
        (click)="close()">
        <svg lucideX class="h-5 w-5"></svg>
      </button>
    </header>

    <div class="flex min-h-0 flex-1">
      <div class="bg-dialog-background flex min-w-0 flex-1 flex-col">
        <div
          class="border-border flex h-15 shrink-0 items-center gap-3 border-b px-4">
          <app-filter-input
            class="w-56 shrink-0"
            [value]="query()"
            (valueChange)="query.set($event)"
            [placeholder]="filterPlaceholder" />

          <app-segmented-control
            variant="chips"
            [options]="filters()"
            [value]="filter()"
            (valueChange)="filter.set($event)"
            [ariaLabel]="filterGroupLabel" />

          <span class="flex-1"></span>

          <app-segmented-control
            variant="outlined"
            [options]="modes()"
            [value]="mode()"
            (valueChange)="setMode($event)"
            [ariaLabel]="modeGroupLabel" />
        </div>

        @if (groups().length === 0) {
          <app-empty-state
            class="flex flex-1 items-center justify-center"
            [title]="emptyTitle"
            [description]="emptyDescription" />
        } @else {
          <main
            class="grid min-h-0 flex-1"
            [style.grid-template-columns]="listColumns()">
            <div class="border-border relative flex min-h-0 flex-col border-r">
              <app-ai-assistant-resize-handle
                [edge]="'right'"
                [width]="listWidth()"
                [minWidth]="minListWidth"
                [maxWidth]="maxListWidth"
                [label]="listResizeLabel"
                (widthChange)="setListWidth($event)"
                (resizingChange)="onListResizing($event)" />

              <div
                class="border-border flex h-11 shrink-0 items-center justify-between gap-2 border-b px-3.5">
                <span class="text-muted text-[13px]">{{ listSummary() }}</span>
                @if (isPending() && !isRunning() && selectableCount() > 1) {
                  <app-button
                    color="neutral"
                    class="-my-1 h-7 px-2 text-[13px]"
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
                  </app-button>
                }
              </div>

              <div class="custom-scroll flex-1 overflow-y-auto pb-3">
                <app-ai-assistant-review-list
                  [groups]="batched()"
                  [excludedChangeIds]="assistant.excludedChangeIds()"
                  [collapsedKeys]="collapsedKeys()"
                  [selectedKey]="selectedGroup()?.key ?? null"
                  [isPending]="isPending()"
                  [isApplying]="isRunning()"
                  [applyStatuses]="assistant.applyStatuses()"
                  [applyingChangeId]="assistant.applyingChangeId()"
                  (selected)="selectedKey.set($event)"
                  (groupToggled)="toggleGroup($event)" />
              </div>
            </div>

            @if (selectedGroup(); as group) {
              <app-ai-assistant-review-detail
                [group]="group"
                [excludedChangeIds]="assistant.excludedChangeIds()"
                [mode]="mode()"
                [isPending]="isPending()"
                [isApplying]="assistant.isApplying()"
                [canRevise]="!isReadOnly"
                [workspace]="workspace()"
                [editingField]="editingField()"
                [editError]="editError()"
                [isSaving]="assistant.isEditingChange()"
                (applied)="applyOnly($event)"
                (toggled)="toggleIncluded($event)"
                (editStarted)="startEditing($event)"
                (editCancelled)="stopEditing()"
                (saved)="saveEdit($event)"
                (revised)="reviseChange($event)" />
            }
          </main>
        }

        @if (isRunning()) {
          <app-progress-bar
            class="h-0.5"
            [rounded]="false"
            [value]="assistant.applyPercent()"
            [mode]="
              assistant.applyTotal() === 0 ? 'indeterminate' : 'determinate'
            " />
        }

        <footer
          class="border-border bg-card-header flex items-center gap-4 border-t px-4 py-3">
          @if (isRunning()) {
            <app-spinner-icon class="shrink-0" />
            <p class="m-0 shrink-0 text-sm font-medium">
              {{ applyingCount() }}
            </p>
            @if (applyingLabel(); as label) {
              <p
                class="text-muted m-0 min-w-0 truncate text-sm"
                [title]="label">
                {{ label }}
              </p>
            }
            <span class="flex-1"></span>
            <button
              app-stroked-button
              class="h-12"
              type="button"
              [disabled]="isStoppingApply()"
              (click)="stopApplying()">
              @if (isStoppingApply()) {
                <span i18n="Shown on the stop button once a stop was asked for"
                  >Stopping…</span
                >
              } @else {
                <span
                  i18n="
                    Button that stops changes part way through being applied
                  "
                  >Stop</span
                >
              }
            </button>
          } @else {
            @if (isPending()) {
              <div class="text-muted flex items-center gap-4 text-[13px]">
                <span class="flex items-center gap-1.5">
                  <app-keyboard-key
                    class="min-w-6 px-2 py-1 text-[13px]"
                    i18n="
                      Keyboard key that moves down the review list. Leave the
                      letter as-is
                    ">
                    j
                  </app-keyboard-key>
                  <app-keyboard-key
                    class="min-w-6 px-2 py-1 text-[13px]"
                    i18n="
                      Keyboard key that moves up the review list. Leave the
                      letter as-is
                    ">
                    k
                  </app-keyboard-key>
                  <span i18n="Keyboard hint for moving through the review list">
                    move
                  </span>
                </span>
                <span class="flex items-center gap-1.5">
                  <app-keyboard-key
                    class="min-w-6 px-2 py-1 text-[13px]"
                    i18n="
                      Name of the space bar. Translate it to its local name
                    ">
                    space
                  </app-keyboard-key>
                  <span i18n="Keyboard hint for including a change"
                    >include</span
                  >
                </span>
                <span class="flex items-center gap-1.5">
                  <app-keyboard-key
                    class="min-w-6 px-2 py-1 text-[13px]"
                    i18n="
                      Keyboard key that edits the selected change. Leave the
                      letter as-is
                    ">
                    e
                  </app-keyboard-key>
                  <span i18n="Keyboard hint for editing a change">edit</span>
                </span>
                <span class="flex items-center gap-1.5">
                  <app-keyboard-key
                    class="min-w-6 px-2 py-1 text-[13px]"
                    i18n="Symbol for the return key. Leave the symbol as-is">
                    &#9166;
                  </app-keyboard-key>
                  <span i18n="Keyboard hint for applying the selected changes">
                    apply selected
                  </span>
                </span>
              </div>
            }

            <span class="flex-1"></span>
            <p class="text-muted m-0 text-sm">{{ status() }}</p>

            <div class="flex items-center gap-2">
              @if (isPending()) {
                <button
                  app-stroked-button
                  class="h-12"
                  type="button"
                  (click)="discard()">
                  <span i18n="Button that discards the proposed changes"
                    >Discard</span
                  >
                </button>
                <button
                  app-flat-button
                  class="h-12"
                  type="button"
                  [disabled]="assistant.isApplying() || selectedCount() === 0"
                  (click)="apply()">
                  <span i18n="Button that applies the proposed changes"
                    >Apply</span
                  >
                  <span>&nbsp;({{ selectedCount() }})</span>
                </button>
              } @else {
                @if (failedCount() > 0 && !isReadOnly) {
                  <button
                    app-stroked-button
                    class="h-12"
                    type="button"
                    [disabled]="assistant.isApplying()"
                    (click)="retryFailed()">
                    <span i18n="Button that runs the changes that failed again">
                      Retry failed
                    </span>
                    <span>&nbsp;({{ failedCount() }})</span>
                  </button>
                }
                @if (canUndo()) {
                  <button
                    app-stroked-button
                    class="h-12"
                    type="button"
                    [disabled]="assistant.isApplying()"
                    (click)="undo()">
                    <span i18n="Button that takes back an applied change set"
                      >Undo</span
                    >
                  </button>
                }
                <button
                  app-stroked-button
                  class="h-12"
                  type="button"
                  (click)="close()">
                  <span i18n="Button that closes the proposed changes dialog"
                    >Close</span
                  >
                </button>
              }
            </div>
          }
        </footer>
      </div>

      @if (canChat && isAssistantOpen()) {
        <app-ai-assistant-panel
          class="border-border shrink-0 border-l"
          [style.width]="assistantWidth()"
          variant="review"
          (closed)="isAssistantOpen.set(false)" />
      }
    </div>
  `,
})
export class AiAssistantReviewDialogComponent {
  protected readonly assistant = inject(AiAssistantService);

  private readonly panel = inject(AiPanelService);
  private readonly dialogRef = inject<DialogRef<void>>(DialogRef);
  private readonly router = inject(Router);
  private readonly data = inject<AiReviewData | null>(DIALOG_DATA, {
    optional: true,
  });

  /** A change set handed in is history: it is read back, never decided on. */
  protected readonly isReadOnly = !!this.data?.changeSet;

  /** The live review keeps the chat beside it so a revision never leaves the review. */
  protected readonly canChat = !this.isReadOnly && this.panel.isAvailable();
  protected readonly isAssistantOpen = signal(this.canChat);

  private readonly assistantPanel = viewChild(AiAssistantPanelComponent);

  protected readonly assistantWidth = computed(() => {
    return `min(${this.panel.width()}px, 40vw)`;
  });

  protected readonly selectedKey = signal<string | null>(null);
  protected readonly filter = signal<AiReviewFilter>(
    this.data?.filter ?? 'all'
  );
  protected readonly query = signal('');
  protected readonly collapsedKeys = signal<ReadonlySet<string>>(new Set());
  protected readonly editingField = signal<AiFieldTarget | null>(null);
  protected readonly editError = signal<string | null>(null);
  protected readonly mode = signal<AiDiffMode>(this.storedMode());
  protected readonly listWidth = signal(this.storedListWidth());

  protected readonly minListWidth = MIN_LIST_WIDTH;
  protected readonly maxListWidth = MAX_LIST_WIDTH;
  protected readonly listResizeLabel = $localize`:Accessible name of the handle that resizes the list of changes in the review:Resize change list`;

  protected readonly listColumns = computed(() => {
    return `${this.listWidth()}px minmax(0, 1fr)`;
  });

  protected readonly conversationTitle = this.assistant.conversationTitle;

  protected readonly filterPlaceholder = $localize`:Placeholder of the field that filters the review list:Filter changes`;
  protected readonly filterGroupLabel = $localize`:Accessible label for the switch that narrows the review list:Filter changes`;
  protected readonly modeGroupLabel = $localize`:Accessible label for the diff layout switch:Diff layout`;
  protected readonly emptyTitle = $localize`:Shown when there is no change to review:There is nothing to review.`;
  protected readonly emptyDescription = $localize`:Explains the empty review surface:Ask the assistant to propose changes and they will show up here for approval.`;

  protected readonly title = computed(() => {
    if (this.isReadOnly) {
      return $localize`:Title of the full screen view of changes already made:Applied changes`;
    }

    return $localize`:Title of the full screen review of proposed changes:Review changes`;
  });

  protected readonly isStoppingApply = this.assistant.isStoppingApply;

  /** Only the live change set is ever being applied; a set handed in is history. */
  protected readonly isRunning = computed(() => {
    const changeSet = this.changeSet();

    return (
      changeSet !== null &&
      this.assistant.applyingChangeSetId() === changeSet.id
    );
  });

  protected readonly applyingCount = computed(() => {
    const completed = this.assistant.applyCompleted();
    const total = this.assistant.applyTotal();

    return $localize`:Counts the changes a run has applied so far:Applying ${completed}:COMPLETED: of ${total}:TOTAL:`;
  });

  protected readonly applyingLabel = computed(() => {
    const changeId = this.assistant.applyingChangeId();
    const change = this.changes().find(
      (candidate) => candidate.id === changeId
    );

    if (!change) {
      return '';
    }

    const summary = changeSummary(change);

    return summary.target ?? change.summary;
  });

  protected readonly changeSet = computed<AiChangeSet | null>(() => {
    return this.data?.changeSet ?? this.assistant.changeSet();
  });

  protected readonly workspace = computed(() => {
    if (this.isReadOnly) {
      return this.data?.workspace ?? null;
    }

    return this.assistant.workspaceKey();
  });

  protected readonly changes = computed(() => {
    return this.changeSet()?.changes ?? [];
  });

  protected readonly isPending = computed(() => {
    const isPending = this.changeSet()?.status === AiChangeSetStatus.pending;

    return isPending && !this.isReadOnly;
  });

  protected readonly visibleChanges = computed(() => {
    const filter = this.filter();
    const query = this.query().trim().toLowerCase();

    return this.changes().filter((change) => {
      const matchesQuery =
        query.length === 0 || change.summary.toLowerCase().includes(query);

      return matchesQuery && this.matchesFilter(change, filter);
    });
  });

  protected readonly groups = computed<AiChangeGroup[]>(() => {
    return groupChanges(this.visibleChanges());
  });

  protected readonly batched = computed(() => batchGroups(this.groups()));

  /** The list order the keyboard walks, which is the order the rows render in. */
  private readonly orderedGroups = computed(() => {
    const { batches, rest } = this.batched();

    return [...batches.flatMap((batch) => batch.groups), ...rest];
  });

  protected readonly selectedGroup = computed<AiChangeGroup | null>(() => {
    const ordered = this.orderedGroups();
    const selectedKey = this.selectedKey();

    return (
      ordered.find((group) => group.key === selectedKey) ?? ordered[0] ?? null
    );
  });

  protected readonly selectable = computed(() => {
    return this.changes().filter(isValid);
  });
  protected readonly selectableCount = computed(() => this.selectable().length);

  protected readonly selectedCount = computed(() => {
    const excluded = this.assistant.excludedChangeIds();

    return this.selectable().filter((change) => !excluded.has(change.id))
      .length;
  });

  protected readonly isEveryChangeSelected = computed(() => {
    return this.selectedCount() === this.selectableCount();
  });

  protected readonly failedCount = computed(() => {
    const changeSet = this.changeSet();
    const isUndone =
      changeSet?.undoneAt !== null && changeSet?.undoneAt !== undefined;

    if (isUndone) {
      return 0;
    }

    return this.changes().filter((change) => {
      return change.applyStatus === AiChangeApplyStatus.failed;
    }).length;
  });

  protected readonly canUndo = computed(() => {
    const changeSet = this.changeSet();
    const isUndone = !!changeSet?.undoneAt;

    if (this.isPending() || this.isReadOnly || isUndone) {
      return false;
    }

    return this.changes().some((change) => {
      return isApplied(change) && change.canUndo && !change.undoneAt;
    });
  });

  protected readonly filters = computed<SegmentedOption<AiReviewFilter>[]>(
    () => {
      const changes = this.changes();
      const count = (filter: AiReviewFilter) => {
        return changes.filter((change) => this.matchesFilter(change, filter))
          .length;
      };

      const failed = count('failed');
      const options: SegmentedOption<AiReviewFilter>[] = [
        {
          value: 'all',
          label: $localize`:Review filter showing every change:All`,
          count: changes.length,
        },
        {
          value: 'created',
          label: $localize`:Review filter showing creations:New`,
          count: count('created'),
        },
        {
          value: 'updated',
          label: $localize`:Review filter showing updates:Updated`,
          count: count('updated'),
        },
        {
          value: 'removed',
          label: $localize`:Review filter showing deletions:Removed`,
          count: count('removed'),
        },
        {
          value: 'blocked',
          label: $localize`:Review filter showing changes that cannot be applied:Blocked`,
          count: count('blocked'),
        },
      ];

      if (failed === 0) {
        return options;
      }

      return [
        ...options,
        {
          value: 'failed' as AiReviewFilter,
          label: $localize`:Review filter showing changes that could not be applied:Failed`,
          count: failed,
        },
      ];
    }
  );

  protected readonly modes = computed<SegmentedOption<AiDiffMode>[]>(() => [
    {
      value: 'split' as AiDiffMode,
      label: $localize`:Diff layout with two columns:Split`,
    },
    {
      value: 'unified' as AiDiffMode,
      label: $localize`:Diff layout with one column:Unified`,
    },
    {
      value: 'inline' as AiDiffMode,
      label: $localize`:Diff layout highlighting changed words:Inline`,
    },
  ]);

  protected readonly listSummary = computed(() => {
    const changes = this.visibleChanges().length;
    const groups = this.groups().length;

    return $localize`:Counts the changes and the entities they touch:${changes}:CHANGES: changes across ${groups}:GROUPS: entities`;
  });

  protected readonly status = computed(() => {
    const total = this.changes().length;
    const changeSet = this.changeSet();

    if (this.isPending()) {
      return $localize`:Counts the proposals that will be applied:${this.selectedCount()}:SELECTED: of ${total}:TOTAL: changes selected`;
    }

    if (changeSet?.status === AiChangeSetStatus.discarded) {
      return $localize`:Shown after proposals were discarded:These changes were discarded.`;
    }

    const undone = this.changes().filter((change) => change.undoneAt).length;

    if (undone > 0) {
      return $localize`:Shown after applied proposals were taken back:${undone}:UNDONE: of these changes were undone.`;
    }

    const applied = this.changes().filter(isApplied).length;
    const failed = this.failedCount();

    if (failed > 0) {
      return $localize`:Shown after a change set was partly applied:${applied}:APPLIED: of ${total}:TOTAL: applied. ${failed}:FAILED: failed.`;
    }

    return $localize`:Counts the proposals that were applied:${applied}:APPLIED: of ${total}:TOTAL: changes applied`;
  });

  constructor() {
    const openedPath = pathOf(this.router.url);

    this.router.events.pipe(takeUntilDestroyed()).subscribe((event) => {
      const isNewPage =
        event instanceof NavigationEnd &&
        pathOf(event.urlAfterRedirects) !== openedPath;

      if (isNewPage) {
        this.close();
      }
    });

    effect(() => {
      const isStale = this.filter() === 'failed' && this.failedCount() === 0;

      if (isStale) {
        this.filter.set('all');
      }
    });

    effect(() => {
      const selected = this.selectedGroup();

      if (selected && selected.key !== this.selectedKey()) {
        this.selectedKey.set(selected.key);
      }
    });

    effect(() => {
      this.selectedKey();

      untracked(() => this.stopEditing());
    });
  }

  protected setQuery(event: Event) {
    this.query.set((event.target as HTMLInputElement).value);
  }

  protected setMode(mode: AiDiffMode) {
    this.mode.set(mode);

    try {
      localStorage.setItem(MODE_KEY, mode);
    } catch {
      // Ignore storage failures (private mode, quota, etc.).
    }
  }

  protected setListWidth(width: number) {
    this.listWidth.set(clampListWidth(width));
  }

  /** A drag writes once when it lets go rather than on every pointer move. */
  protected onListResizing(isResizing: boolean) {
    if (isResizing) {
      return;
    }

    try {
      localStorage.setItem(LIST_WIDTH_KEY, String(this.listWidth()));
    } catch {
      // Ignore storage failures (private mode, quota, etc.).
    }
  }

  protected toggleGroup(key: string) {
    toggleInSet(this.collapsedKeys, key);
  }

  protected toggleAll() {
    const excluded = this.assistant.excludedChangeIds();
    const shouldClear = this.isEveryChangeSelected();
    const changed = this.selectable()
      .filter((change) => excluded.has(change.id) !== shouldClear)
      .map((change) => change.id);

    this.assistant.toggleChanges(changed);
  }

  protected async apply() {
    await this.assistant.applyChangeSet();
  }

  protected async stopApplying() {
    await this.assistant.stopApplying();
  }

  /** Applying one entity's proposals is the whole set with everything else left out. */
  protected async applyOnly(changeIds: number[]) {
    const kept = new Set(changeIds);
    const excluded = this.assistant.excludedChangeIds();
    const changed = this.selectable()
      .filter((change) => {
        const shouldExclude = !kept.has(change.id);

        return excluded.has(change.id) !== shouldExclude;
      })
      .map((change) => change.id);

    this.assistant.toggleChanges(changed);

    await this.assistant.applyChangeSet();
  }

  /** An entity is in or out as a whole: any change left in means the switch reads as on. */
  protected toggleIncluded(changeIds: number[]) {
    const excluded = this.assistant.excludedChangeIds();
    const isIncluded = changeIds.some((id) => !excluded.has(id));
    const changed = changeIds.filter((id) => excluded.has(id) !== isIncluded);

    this.assistant.toggleChanges(changed);
  }

  protected startEditing(target: AiFieldTarget) {
    this.editError.set(null);
    this.editingField.set(target);
  }

  protected stopEditing() {
    this.editError.set(null);
    this.editingField.set(null);
  }

  protected async saveEdit({ changeId, name, value }: AiFieldEdit) {
    const error = await this.assistant.updateChange(changeId, [
      { name, value },
    ]);

    this.editError.set(error);

    if (error === null) {
      this.editingField.set(null);
    }
  }

  /** The correction itself is typed in the docked composer; the change goes with it as context. */
  protected reviseChange(changeId: number) {
    const change = this.changes().find(
      (candidate) => candidate.id === changeId
    );

    if (!change) {
      return;
    }

    const target = changeSummary(change).target ?? change.summary;
    const prefix = $localize`:Seeds the composer with a request to rework one proposed change:Rework this proposal: `;

    this.assistant.reviseChange(
      changeId,
      `${prefix}${changeLetter(change)} ${target} — `
    );
    this.isAssistantOpen.set(true);

    setTimeout(() => this.assistantPanel()?.focusComposer());
  }

  protected async retryFailed() {
    await this.assistant.retryFailedChanges();
  }

  protected async undo() {
    await this.assistant.undoChangeSet();
  }

  protected async discard() {
    await this.assistant.discardChangeSet();

    this.close();
  }

  protected close() {
    this.dialogRef.close();
  }

  protected onKeydown(event: KeyboardEvent) {
    if (this.isRunning()) {
      return;
    }

    const target = event.target as HTMLElement | null;
    const isTyping =
      target?.tagName === 'INPUT' || target?.tagName === 'TEXTAREA';
    const isInAssistant = !!target?.closest('app-ai-assistant-panel');

    if ((isTyping && event.key !== 'Escape') || isInAssistant) {
      return;
    }

    if (event.key === 'j' || event.key === 'ArrowDown') {
      this.step(1);
      event.preventDefault();

      return;
    }

    if (event.key === 'k' || event.key === 'ArrowUp') {
      this.step(-1);
      event.preventDefault();

      return;
    }

    const selected = this.selectedGroup();

    if (!selected) {
      return;
    }

    const ids = selectableIds(selected);

    if (event.key === ' ' && this.isPending() && ids.length > 0) {
      this.toggleIncluded(ids);
      event.preventDefault();

      return;
    }

    if (event.key === 'e' && this.isPending()) {
      const change = selected.changes.find((candidate) => {
        return candidate.fields.some(isTextField);
      });
      const field = change?.fields.find(isTextField);

      if (change && field) {
        this.startEditing({ changeId: change.id, name: field.name });
      }

      event.preventDefault();

      return;
    }

    if (event.key === 'Enter' && this.isPending() && this.selectedCount() > 0) {
      void this.apply();
      event.preventDefault();
    }
  }

  private step(offset: number) {
    const ordered = this.orderedGroups();

    if (ordered.length === 0) {
      return;
    }

    const current = ordered.findIndex((group) => {
      return group.key === this.selectedKey();
    });
    const next = Math.min(Math.max(current + offset, 0), ordered.length - 1);

    this.selectedKey.set(ordered[next].key);
  }

  private matchesFilter(
    change: AiProposedChange,
    filter: AiReviewFilter
  ): boolean {
    if (filter === 'all') {
      return true;
    }

    if (filter === 'blocked') {
      return !isValid(change);
    }

    if (filter === 'failed') {
      return change.applyStatus === AiChangeApplyStatus.failed;
    }

    const letter = changeLetter(change);

    if (filter === 'created') {
      return letter === 'A';
    }

    if (filter === 'removed') {
      return letter === 'D';
    }

    return letter === 'M';
  }

  private storedMode(): AiDiffMode {
    const stored = this.readMode();
    const isKnown =
      stored === 'split' || stored === 'unified' || stored === 'inline';

    return isKnown ? stored : 'split';
  }

  private storedListWidth(): number {
    try {
      const stored = localStorage.getItem(LIST_WIDTH_KEY);

      return stored === null ? DEFAULT_LIST_WIDTH : clampListWidth(+stored);
    } catch {
      return DEFAULT_LIST_WIDTH;
    }
  }

  private readMode(): string | null {
    try {
      return localStorage.getItem(MODE_KEY);
    } catch {
      return null;
    }
  }
}
