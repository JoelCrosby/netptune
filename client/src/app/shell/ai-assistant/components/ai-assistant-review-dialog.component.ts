import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, effect, inject, signal } from '@angular/core';
import {
  AiChangeApplyStatus,
  AiChangeField,
  AiChangeSet,
  AiChangeSetStatus,
  AiChangeValueKind,
  AiProposedChange,
} from '@core/models/ai-conversation';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { LucideX } from '@lucide/angular';
import { ButtonComponent } from '@static/components/button/button.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';
import { KeyboardKeyComponent } from '@static/components/keyboard-key/keyboard-key.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import {
  AiChangeGroup,
  groupChanges,
  isApplied,
  isValid,
} from './ai-assistant-change-group';
import { changeSummary } from './ai-assistant-change-kind';
import { AiDiffMode, changeLetter } from './ai-assistant-diff';
import {
  AiAssistantReviewDetailComponent,
  AiFieldEdit,
} from './ai-assistant-review-detail.component';
import { AiAssistantReviewListComponent } from './ai-assistant-review-list.component';

type AiReviewFilter = 'all' | 'created' | 'updated' | 'removed' | 'blocked';

/**
 * Opening the review without data reviews the conversation's live change set.
 * A change set handed in is a record of what already happened, so the surface
 * reads it back without offering a decision.
 */
export interface AiReviewData {
  changeSet: AiChangeSet;
  workspace: string | null;
}

const MODE_KEY = 'netptune.ai.review.mode';

const isTextField = (field: AiChangeField): boolean => {
  return field.kind === AiChangeValueKind.text;
};

/**
 * Full screen review of a change set, laid out the way a source control view
 * lays out a commit: what changed on the left, the diff of the selection on the
 * right, the decision along the bottom.
 */
@Component({
  selector: 'app-ai-assistant-review-dialog',
  host: {
    class: 'flex h-full min-h-0 flex-col text-sm',
    '(document:keydown)': 'onKeydown($event)',
  },
  imports: [
    LucideX,
    ButtonComponent,
    FlatButtonComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    EmptyStateComponent,
    FilterInputComponent,
    KeyboardKeyComponent,
    SegmentedControlComponent,
    AiAssistantReviewDetailComponent,
    AiAssistantReviewListComponent,
  ],
  template: `
    <header
      class="border-border bg-card-header flex items-center gap-4 border-b py-2 pr-3 pl-2">
      <button
        app-icon-button
        class="h-9 w-9"
        type="button"
        i18n-aria-label="Accessible label for the button that closes the review"
        aria-label="Close review"
        (click)="close()">
        <svg lucideX class="h-[18px] w-[18px]"></svg>
      </button>

      <div class="flex min-w-0 items-baseline gap-2.5">
        <h1 class="font-overpass m-0 text-base font-medium whitespace-nowrap">
          {{ title() }}
        </h1>
        <span class="text-muted truncate text-xs">{{
          conversationTitle()
        }}</span>
      </div>
    </header>

    <div class="border-border flex items-center gap-3 border-b px-3 py-2">
      <app-filter-input
        class="min-w-[220px]"
        [value]="query()"
        (valueChange)="query.set($event)"
        [placeholder]="filterPlaceholder" />

      <app-segmented-control
        [options]="filters()"
        [value]="filter()"
        (valueChange)="filter.set($event)"
        [ariaLabel]="filterGroupLabel" />

      <span class="flex-1"></span>

      <app-segmented-control
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
      <main class="grid min-h-0 flex-1 grid-cols-[360px_minmax(0,1fr)]">
        <div class="border-border bg-card flex min-h-0 flex-col border-r">
          <div
            class="border-border flex items-center justify-between gap-2 border-b px-3 py-2">
            <span class="text-muted text-xs">{{ listSummary() }}</span>
            @if (isPending() && selectableCount() > 1) {
              <app-button
                color="neutral"
                class="-my-1 h-7 px-2 text-xs"
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
              [groups]="groups()"
              [excludedChangeIds]="assistant.excludedChangeIds()"
              [collapsedKeys]="collapsedKeys()"
              [selectedChangeId]="selectedChangeId()"
              [isPending]="isPending()"
              (selected)="selectedChangeId.set($event)"
              (toggled)="assistant.toggleChange($event)"
              (groupToggled)="toggleGroup($event)" />
          </div>

          <div
            class="border-border text-muted flex items-center gap-3 border-t px-3 py-2 text-[11px]">
            <span class="flex items-center gap-1.5">
              <span
                class="font-avatar text-change-added font-bold"
                i18n="
                  Single letter marking a change that creates something. It
                  labels the rows in the list, so leave the letter as-is
                ">
                A
              </span>
              <span i18n="Legend for changes that create something">new</span>
            </span>
            <span class="flex items-center gap-1.5">
              <span
                class="font-avatar text-change-modified font-bold"
                i18n="
                  Single letter marking a change that updates something. It
                  labels the rows in the list, so leave the letter as-is
                ">
                M
              </span>
              <span i18n="Legend for changes that update something"
                >updated</span
              >
            </span>
            <span class="flex items-center gap-1.5">
              <span
                class="font-avatar text-change-removed font-bold"
                i18n="
                  Single letter marking a change that removes something. It
                  labels the rows in the list, so leave the letter as-is
                ">
                D
              </span>
              <span i18n="Legend for changes that remove something"
                >removed</span
              >
            </span>
          </div>
        </div>

        @if (selectedChange(); as change) {
          <app-ai-assistant-review-detail
            [change]="change"
            [mode]="mode()"
            [isPending]="isPending()"
            [isApplying]="assistant.isApplying()"
            [canRevise]="!isReadOnly"
            [workspace]="workspace()"
            [editingField]="editingField()"
            [editError]="editError()"
            [isSaving]="assistant.isEditingChange()"
            (applied)="applyOne($event)"
            (editStarted)="startEditing($event)"
            (editCancelled)="stopEditing()"
            (saved)="saveEdit(change.id, $event)"
            (revised)="reviseChange($event)" />
        }
      </main>
    }

    <footer
      class="border-border bg-card-header flex items-center gap-4 border-t px-3.5 py-2">
      @if (isPending()) {
        <div class="text-muted flex items-center gap-3.5 text-[11px]">
          <span class="flex items-center gap-1.5">
            <app-keyboard-key
              i18n="
                Keyboard key that moves down the review list. Leave the letter
                as-is
              ">
              j
            </app-keyboard-key>
            <app-keyboard-key
              i18n="
                Keyboard key that moves up the review list. Leave the letter
                as-is
              ">
              k
            </app-keyboard-key>
            <span i18n="Keyboard hint for moving through the review list">
              move
            </span>
          </span>
          <span class="flex items-center gap-1.5">
            <app-keyboard-key
              i18n="Name of the space bar. Translate it to its local name">
              space
            </app-keyboard-key>
            <span i18n="Keyboard hint for including a change">include</span>
          </span>
          <span class="flex items-center gap-1.5">
            <app-keyboard-key
              i18n="
                Keyboard key that edits the selected change. Leave the letter
                as-is
              ">
              e
            </app-keyboard-key>
            <span i18n="Keyboard hint for editing a change">edit</span>
          </span>
          <span class="flex items-center gap-1.5">
            <app-keyboard-key
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
      <p class="text-muted m-0 text-xs">{{ status() }}</p>

      <div class="flex items-center gap-2">
        @if (isPending()) {
          <button app-stroked-button type="button" (click)="discard()">
            <span i18n="Button that discards the proposed changes"
              >Discard</span
            >
          </button>
          <button
            app-flat-button
            type="button"
            [disabled]="assistant.isApplying() || selectedCount() === 0"
            (click)="apply()">
            <span i18n="Button that applies the proposed changes">Apply</span>
            <span>&nbsp;({{ selectedCount() }})</span>
          </button>
        } @else {
          @if (failedCount() > 0 && !isReadOnly) {
            <button
              app-stroked-button
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
              type="button"
              [disabled]="assistant.isApplying()"
              (click)="undo()">
              <span i18n="Button that takes back an applied change set"
                >Undo</span
              >
            </button>
          }
          <button app-stroked-button type="button" (click)="close()">
            <span i18n="Button that closes the proposed changes dialog"
              >Close</span
            >
          </button>
        }
      </div>
    </footer>
  `,
})
export class AiAssistantReviewDialogComponent {
  protected readonly assistant = inject(AiAssistantService);

  private readonly dialogRef = inject<DialogRef<void>>(DialogRef);
  private readonly data = inject<AiReviewData | null>(DIALOG_DATA, {
    optional: true,
  });

  /** A change set handed in is history: it is read back, never decided on. */
  protected readonly isReadOnly = this.data !== null;

  protected readonly selectedChangeId = signal<number | null>(null);
  protected readonly filter = signal<AiReviewFilter>('all');
  protected readonly query = signal('');
  protected readonly collapsedKeys = signal<Set<string>>(new Set());
  protected readonly editingField = signal<string | null>(null);
  protected readonly editError = signal<string | null>(null);
  protected readonly mode = signal<AiDiffMode>(this.storedMode());

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

  protected readonly changeSet = computed<AiChangeSet | null>(() => {
    return this.data?.changeSet ?? this.assistant.changeSet();
  });

  protected readonly workspace = computed(() => {
    return this.data ? this.data.workspace : this.assistant.workspaceKey();
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

  /** The list order the keyboard walks, which is the order the groups render in. */
  private readonly orderedChanges = computed(() => {
    return this.groups().flatMap((group) => group.changes);
  });

  protected readonly selectedChange = computed<AiProposedChange | null>(() => {
    const ordered = this.orderedChanges();
    const selectedId = this.selectedChangeId();

    return (
      ordered.find((change) => change.id === selectedId) ?? ordered[0] ?? null
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

      return [
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
    effect(() => {
      const selected = this.selectedChange();

      if (selected && selected.id !== this.selectedChangeId()) {
        this.selectedChangeId.set(selected.id);
        this.editingField.set(null);
        this.editError.set(null);
      }
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

  protected toggleGroup(key: string) {
    this.collapsedKeys.update((current) => {
      const next = new Set(current);

      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }

      return next;
    });
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

  /** Applying one proposal is the whole set with everything else left out. */
  protected async applyOne(changeId: number) {
    const excluded = this.assistant.excludedChangeIds();
    const changed = this.selectable()
      .filter((change) => {
        const shouldExclude = change.id !== changeId;

        return excluded.has(change.id) !== shouldExclude;
      })
      .map((change) => change.id);

    this.assistant.toggleChanges(changed);

    await this.assistant.applyChangeSet();
  }

  protected startEditing(name: string) {
    this.editError.set(null);
    this.editingField.set(name);
  }

  protected stopEditing() {
    this.editError.set(null);
    this.editingField.set(null);
  }

  protected async saveEdit(changeId: number, edit: AiFieldEdit) {
    const error = await this.assistant.updateChange(changeId, [edit]);

    this.editError.set(error);

    if (error === null) {
      this.editingField.set(null);
    }
  }

  /** The correction itself is typed in the composer; the change goes with it as context. */
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
    this.close();
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
    const target = event.target as HTMLElement | null;
    const isTyping =
      target?.tagName === 'INPUT' || target?.tagName === 'TEXTAREA';

    if (isTyping && event.key !== 'Escape') {
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

    const selected = this.selectedChange();

    if (!selected) {
      return;
    }

    if (event.key === ' ' && this.isPending() && isValid(selected)) {
      this.assistant.toggleChange(selected.id);
      event.preventDefault();

      return;
    }

    if (event.key === 'e' && this.isPending()) {
      const field = selected.fields.find(isTextField);

      if (field) {
        this.startEditing(field.name);
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
    const ordered = this.orderedChanges();

    if (ordered.length === 0) {
      return;
    }

    const current = ordered.findIndex((change) => {
      return change.id === this.selectedChangeId();
    });
    const next = Math.min(Math.max(current + offset, 0), ordered.length - 1);

    this.selectedChangeId.set(ordered[next].id);
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

  private readMode(): string | null {
    try {
      return localStorage.getItem(MODE_KEY);
    } catch {
      return null;
    }
  }
}
