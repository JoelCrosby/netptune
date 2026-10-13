import { Component, computed, input } from '@angular/core';
import { AiChangeField } from '@core/models/ai-conversation';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { ValueChipComponent } from '@static/components/value-chip/value-chip.component';
import { valueDiff } from './ai-assistant-change-kind';
import { toRelatedTask } from './ai-assistant-field-diff';

@Component({
  selector: 'app-ai-assistant-relation-diff',
  host: { class: 'flex flex-col gap-2 px-4 py-3.5' },
  imports: [BadgeComponent, TaskScopeIdComponent, ValueChipComponent],
  template: `
    @for (task of removed(); track task.key) {
      <div class="flex min-w-0 items-center gap-2.5">
        <app-badge shape="rounded" color="warn" class="shrink-0">
          <span i18n="Marks a task link a change removes">Unlink</span>
        </app-badge>
        @if (task.systemId; as systemId) {
          <app-task-scope-id class="shrink-0" [id]="systemId" />
        }
        <app-value-chip tone="removed" [label]="task.name" />
      </div>
    }
    @for (task of added(); track task.key) {
      <div class="flex min-w-0 items-center gap-2.5">
        <app-badge shape="rounded" color="success" class="shrink-0">
          <span i18n="Marks a task link a change adds">Link</span>
        </app-badge>
        @if (task.systemId; as systemId) {
          <app-task-scope-id class="shrink-0" [id]="systemId" />
        }
        <app-value-chip tone="added" [label]="task.name" />
      </div>
    }
  `,
})
export class AiAssistantRelationDiffComponent {
  readonly field = input.required<AiChangeField>();

  private readonly diff = computed(() => valueDiff(this.field()));

  protected readonly removed = computed(() => {
    return this.diff().removed.map(toRelatedTask);
  });

  protected readonly added = computed(() => {
    return this.diff().added.map(toRelatedTask);
  });
}
