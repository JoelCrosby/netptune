import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject } from '@angular/core';
import { AiChangeApplyStatus, AiChangeSet } from '@core/models/ai-conversation';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { AiAssistantChangesTableComponent } from './ai-assistant-changes-table.component';

export interface AiAppliedChangesData {
  changeSet: AiChangeSet;
  workspace: string | null;
}

@Component({
  selector: 'app-ai-assistant-applied-changes-dialog',
  imports: [
    AiAssistantChangesTableComponent,
    DialogActionsDirective,
    DialogTitleComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title
      showCloseButton
      i18n="Title of the dialog listing the changes the assistant carried out">
      Applied changes
    </app-dialog-title>

    <p class="text-muted mb-3 text-sm">{{ summary() }}</p>

    <div class="max-h-[60vh] overflow-auto">
      <app-ai-assistant-changes-table
        [changes]="changes()"
        [excludedChangeIds]="none"
        [isPending]="false"
        [workspace]="data.workspace" />
    </div>

    <div dialogActions align="end">
      <button app-stroked-button type="button" (click)="close()">
        <span i18n="Button that closes the applied changes dialog">Close</span>
      </button>
    </div>
  `,
})
export class AiAssistantAppliedChangesDialogComponent {
  protected readonly data = inject<AiAppliedChangesData>(DIALOG_DATA);

  private readonly dialogRef = inject<DialogRef<void>>(DialogRef);

  protected readonly none = new Set<number>();

  protected readonly changes = computed(() => this.data.changeSet.changes);

  protected readonly summary = computed(() => {
    const changes = this.changes();
    const total = changes.length;
    const isUndone = !!this.data.changeSet.undoneAt;

    if (isUndone) {
      return $localize`:Describes a change set that was taken back:${total}:TOTAL: changes undone`;
    }

    const applied = changes.filter((change) => {
      return change.applyStatus === AiChangeApplyStatus.applied;
    }).length;

    return $localize`:Counts the proposals that were applied:${applied}:APPLIED: of ${total}:TOTAL: changes applied`;
  });

  protected close() {
    this.dialogRef.close();
  }
}
