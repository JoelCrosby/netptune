import { Component, computed, inject, input, viewChild } from '@angular/core';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiAssistantChangeSetComponent } from './components/ai-assistant-change-set.component';
import { AiAssistantComposerComponent } from './components/ai-assistant-composer.component';
import { AiAssistantEmptyStateComponent } from './components/ai-assistant-empty-state.component';
import { AiAssistantHeaderComponent } from './components/ai-assistant-header.component';
import { AiAssistantHistoryComponent } from './components/ai-assistant-history.component';
import { AiAssistantMessageComponent } from './components/ai-assistant-message.component';

@Component({
  selector: 'app-ai-assistant-panel',
  host: { class: 'block h-full min-h-0' },
  imports: [
    AiAssistantChangeSetComponent,
    AiAssistantComposerComponent,
    AiAssistantEmptyStateComponent,
    AiAssistantHeaderComponent,
    AiAssistantHistoryComponent,
    AiAssistantMessageComponent,
  ],
  template: `
    <div
      class="bg-card text-foreground flex h-full w-full flex-col overflow-hidden"
      role="region"
      i18n-aria-label="Accessible name of the AI assistant panel"
      aria-label="AI assistant">
      <app-ai-assistant-header
        [title]="headerTitle()"
        [subtitle]="headerSubtitle()"
        [mode]="assistant.mode()"
        [contentWidth]="contentWidth()"
        [closable]="isDrawer()"
        (historyToggled)="toggleHistory()"
        (modeChange)="setMode($event)"
        (newChat)="startNew()"
        (closed)="assistant.close()" />

      <div class="flex min-h-0 flex-1 flex-col overflow-y-auto">
        <div
          class="mx-auto flex w-full flex-1 flex-col px-4 py-4"
          [class]="contentWidth()">
          @if (assistant.showHistory()) {
            <app-ai-assistant-history
              [conversations]="assistant.conversations()"
              (opened)="openConversation($event)"
              (deleted)="deleteConversation($event)" />
          } @else if (entries().length === 0) {
            <app-ai-assistant-empty-state />
          } @else {
            <div class="flex flex-col gap-5">
              @for (entry of entries(); track $index) {
                <app-ai-assistant-message
                  [entry]="entry"
                  [references]="assistant.references()"
                  [workspace]="assistant.workspaceKey()"
                  [isStreaming]="assistant.isStreaming() && $last" />
              }
            </div>
          }
        </div>
      </div>

      @if (assistant.changeSet(); as proposal) {
        <app-ai-assistant-change-set
          [changeSet]="proposal"
          [excludedChangeIds]="assistant.excludedChangeIds()"
          [isApplying]="assistant.isApplying()"
          [contentWidth]="contentWidth()"
          (toggled)="assistant.toggleChange($event)"
          (applied)="apply()"
          (discarded)="discard()" />
      }

      <app-ai-assistant-composer
        [models]="assistant.models()"
        [selectedModel]="assistant.selectedModel()"
        [modelLabel]="assistant.selectedModelLabel()"
        [isStreaming]="assistant.isStreaming()"
        [contentWidth]="contentWidth()"
        (messageSent)="send($event)"
        (modelSelected)="assistant.selectModel($event)" />
    </div>
  `,
})
export class AiAssistantPanelComponent {
  readonly variant = input<'drawer' | 'page'>('drawer');

  private readonly composer = viewChild.required(AiAssistantComposerComponent);

  protected readonly assistant = inject(AiAssistantService);
  protected readonly entries = computed(() => this.assistant.entries());

  protected readonly isDrawer = computed(() => this.variant() === 'drawer');

  protected readonly contentWidth = computed(() => {
    return this.isDrawer() ? '' : 'max-w-3xl';
  });

  protected readonly headerTitle = computed(() => {
    const title = this.assistant.conversationTitle();

    if (title) {
      return title;
    }

    return $localize`:Header of a conversation that has not started yet:New chat`;
  });

  protected readonly headerSubtitle = computed(() => {
    if (this.assistant.isStreaming()) {
      return $localize`:Assistant subtitle while a reply is streaming:Working on it…`;
    }

    return $localize`:Assistant subtitle above the conversation:How can I help you today?`;
  });

  protected apply() {
    void this.assistant.applyChangeSet();
  }

  protected discard() {
    void this.assistant.discardChangeSet();
  }

  protected setMode(mode: AiDisplayMode) {
    this.assistant.setMode(mode);
  }

  protected toggleHistory() {
    void this.assistant.toggleHistory();
  }

  protected openConversation(conversationId: string) {
    void this.assistant.openConversation(conversationId);
  }

  protected deleteConversation(conversationId: string) {
    void this.assistant.deleteConversation(conversationId);
  }

  protected startNew() {
    this.assistant.startNewConversation();
    this.composer().clear();
  }

  protected send(text: string) {
    void this.assistant.send(text);
  }
}
