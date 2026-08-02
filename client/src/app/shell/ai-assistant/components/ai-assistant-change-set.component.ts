import { Component, computed, inject, input, output } from '@angular/core';
import {
  AiChangeApplyStatus,
  AiChangeSet,
  AiChangeSetStatus,
} from '@core/models/ai-conversation';
import { DialogService } from '@core/services/dialog.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  AiChangeGroup,
  groupChanges,
  isApplied,
  isValid,
} from './ai-assistant-change-group';
import { AiAssistantChangeListComponent } from './ai-assistant-change-list.component';
import { AiAssistantChangesDialogComponent } from './ai-assistant-changes-dialog.component';

/** Enough to read at a glance without pushing the composer off the screen. */
const INLINE_CHANGE_LIMIT = 5;

@Component({
  selector: 'app-ai-assistant-change-set',
  host: { class: 'border-border block border-t' },
  imports: [
    FlatButtonComponent,
    StrokedButtonComponent,
    AiAssistantChangeListComponent,
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

      <app-ai-assistant-change-list
        [groups]="visibleGroups()"
        [excludedChangeIds]="excludedChangeIds()"
        [isPending]="isPending()"
        [workspace]="workspace()"
        (toggled)="toggled.emit($event)" />

      @if (hiddenCount() > 0) {
        <button
          type="button"
          class="text-muted hover:text-foreground mt-2 text-xs underline"
          (click)="showAll()">
          {{ moreLabel() }}
        </button>
      }

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

  private readonly dialog = inject(DialogService);

  protected readonly isPending = computed(() => {
    return this.changeSet().status === AiChangeSetStatus.pending;
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

  protected showAll() {
    this.dialog.open(AiAssistantChangesDialogComponent, { width: '40rem' });
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
