import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject } from '@angular/core';
import { AiChangeSetStatus } from '@core/models/ai-conversation';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { groupChanges } from './ai-assistant-change-group';
import { AiAssistantChangeListComponent } from './ai-assistant-change-list.component';

@Component({
  selector: 'app-ai-assistant-changes-dialog',
  imports: [
    AiAssistantChangeListComponent,
    DialogActionsDirective,
    StrokedButtonComponent,
  ],
  template: `
    <h1
      class="mb-4 text-xl font-semibold"
      i18n="Title of the dialog listing every proposed change">
      Proposed changes
    </h1>

    <div class="max-h-[60vh] overflow-y-auto pr-1">
      <app-ai-assistant-change-list
        [groups]="groups()"
        [excludedChangeIds]="assistant.excludedChangeIds()"
        [isPending]="isPending()"
        [workspace]="assistant.workspaceKey()"
        (toggled)="assistant.toggleChange($event)" />
    </div>

    <div dialogActions>
      <button app-stroked-button type="button" (click)="close()">
        <span i18n="Button that closes the proposed changes dialog">Close</span>
      </button>
    </div>
  `,
})
export class AiAssistantChangesDialogComponent {
  protected readonly assistant = inject(AiAssistantService);

  private readonly dialogRef = inject<DialogRef<void>>(DialogRef);

  protected readonly groups = computed(() => {
    return groupChanges(this.assistant.changeSet()?.changes ?? []);
  });

  protected readonly isPending = computed(() => {
    return this.assistant.changeSet()?.status === AiChangeSetStatus.pending;
  });

  protected close() {
    this.dialogRef.close();
  }
}
