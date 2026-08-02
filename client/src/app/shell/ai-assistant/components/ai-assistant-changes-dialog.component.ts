import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject } from '@angular/core';
import {
  AiChangeApplyStatus,
  AiChangeSetStatus,
} from '@core/models/ai-conversation';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { isApplied, isValid } from './ai-assistant-change-group';
import { AiAssistantChangesTableComponent } from './ai-assistant-changes-table.component';

@Component({
  selector: 'app-ai-assistant-changes-dialog',
  imports: [
    AiAssistantChangesTableComponent,
    DialogActionsDirective,
    DialogTitleComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title
      showCloseButton
      i18n="Title of the dialog listing every proposed change">
      Proposed changes
    </app-dialog-title>

    <div class="mb-3 flex flex-wrap items-center justify-between gap-3">
      <p class="text-muted text-sm">{{ summary() }}</p>

      @if (isPending() && selectableCount() > 1) {
        <button
          type="button"
          class="text-muted hover:text-foreground text-sm"
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

    <div class="max-h-[60vh] overflow-auto">
      <app-ai-assistant-changes-table
        [changes]="changes()"
        [excludedChangeIds]="assistant.excludedChangeIds()"
        [isPending]="isPending()"
        [workspace]="assistant.workspaceKey()"
        (toggled)="assistant.toggleChange($event)"
        (toggledAll)="toggleAll()" />
    </div>

    <div dialogActions align="end">
      @if (isPending()) {
        <button app-stroked-button type="button" (click)="discard()">
          <span i18n="Button that discards the proposed changes">Discard</span>
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
          <span i18n="Button that closes the proposed changes dialog">
            Close
          </span>
        </button>
      }
    </div>
  `,
})
export class AiAssistantChangesDialogComponent {
  protected readonly assistant = inject(AiAssistantService);

  private readonly dialogRef = inject<DialogRef<void>>(DialogRef);

  protected readonly changes = computed(() => {
    return this.assistant.changeSet()?.changes ?? [];
  });

  protected readonly isPending = computed(() => {
    return this.assistant.changeSet()?.status === AiChangeSetStatus.pending;
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

  protected readonly summary = computed(() => {
    const total = this.changes().length;

    if (this.isPending()) {
      const selected = this.selectedCount();

      return $localize`:Counts the proposals that will be applied:${selected}:SELECTED: of ${total}:TOTAL: changes selected`;
    }

    const applied = this.changes().filter((change) => {
      return change.applyStatus === AiChangeApplyStatus.applied;
    }).length;

    return $localize`:Counts the proposals that were applied:${applied}:APPLIED: of ${total}:TOTAL: changes applied`;
  });

  protected readonly canUndo = computed(() => {
    const changeSet = this.assistant.changeSet();
    const isUndone =
      changeSet?.undoneAt !== null && changeSet?.undoneAt !== undefined;

    if (this.isPending() || isUndone) {
      return false;
    }

    return this.changes().some((change) => {
      return isApplied(change) && change.canUndo && !change.undoneAt;
    });
  });

  protected async undo() {
    await this.assistant.undoChangeSet();
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

  protected async discard() {
    await this.assistant.discardChangeSet();

    this.close();
  }

  protected close() {
    this.dialogRef.close();
  }
}
