import { Component, computed, inject, input, viewChild } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { hasPermission } from '@core/auth/has-permission';
import { assistantInlinePrompt } from '@core/util/assistant-inline-prompt';
import { syncedDraft } from '@core/util/synced-draft';
import { ChangeConflictComponent } from '@static/components/change-conflict/change-conflict.component';
import { basicEditorCommands } from '@static/components/editor/editor-commands';
import {
  EditorAppearance,
  EditorComponent,
  EditorUploader,
} from '@static/components/editor/editor.component';
import { EYEBROW } from './task-detail-styles';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-description',
  template: `
    @if (label(); as label) {
      <div [class]="eyebrowClass" [id]="labelId">{{ label }}</div>
    }

    @if (description.hasIncoming()) {
      <app-change-conflict
        class="mb-3 block"
        i18n-title="
          Title of the notice shown when a task description changed elsewhere
          while it was being edited
        "
        title="This description was changed elsewhere"
        (loadIncoming)="description.loadIncoming()"
        (keepMine)="description.keepMine()" />
    }

    <app-editor
      [attr.aria-labelledby]="label() ? labelId : null"
      i18n-placeholder="Placeholder in the empty task description editor"
      placeholder="Add a Description..."
      [appearance]="appearance()"
      [hostClass]="textClass()"
      [uploader]="uploadMedia"
      [commands]="commands"
      [inlinePrompt]="inlinePrompt()"
      (saved)="description.save($event)"
      [finalSave]="finalSave"
      [(value)]="description.value"
      [isReadOnly]="isReadOnly()"></app-editor>
  `,
  imports: [ChangeConflictComponent, EditorComponent],
})
export class TaskDetailDescriptionComponent {
  readonly appearance = input<EditorAppearance>('flat');
  readonly textClass = input('text-[15px]/[26px]');
  readonly label = input<string | null>(null);

  private readonly taskDetail = inject(TaskDetailService);
  private readonly editor = viewChild(EditorComponent);

  readonly task = this.taskDetail.task;
  private readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);

  readonly commands = basicEditorCommands;

  readonly eyebrowClass = `${EYEBROW} mb-2.5`;
  readonly labelId = 'task-detail-description-label';

  readonly uploadMedia: EditorUploader = (file) => {
    return this.taskDetail.uploadMedia(file);
  };

  isReadOnly = computed(() => !this.canUpdate());

  private readonly assistantPrompt = assistantInlinePrompt(
    this.taskDetail.writeDescription
  );

  readonly inlinePrompt = computed(() => {
    const canWrite = this.canUpdate() && this.taskDetail.canAskAssistant();

    return canWrite ? this.assistantPrompt : null;
  });

  readonly description = syncedDraft({
    key: () => this.task()?.id,
    saved: () => this.task()?.description ?? '',
    isEditing: () => this.editor()?.isEditing() ?? false,
    replace: (value) => this.editor()?.discard(value),
    save: (value) => this.taskDetail.updateDescription(value),
  });

  readonly finalSave = (value: string) => this.description.save(value);
}
