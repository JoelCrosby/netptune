import {
  Component,
  DestroyRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import { LucideArrowDown } from '@lucide/angular';
import { MessageScrollerDirective } from '@static/directives/message-scroller.directive';
import { MessageScrollerItemDirective } from '@static/directives/message-scroller-item.directive';
import { AiChatEntry } from '@core/models/ai-chat-entry';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiPanelService } from '@core/services/ai-panel.service';
import { AiAssistantChangeSetComponent } from './components/ai-assistant-change-set.component';
import { AiAssistantComposerComponent } from './components/ai-assistant-composer.component';
import { AiAssistantContextComponent } from './components/ai-assistant-context.component';
import { AiAssistantEmptyStateComponent } from './components/ai-assistant-empty-state.component';
import { AiAssistantHeaderComponent } from './components/ai-assistant-header.component';
import { AiAssistantHistoryComponent } from './components/ai-assistant-history.component';
import { AiAssistantMessageComponent } from './components/ai-assistant-message.component';
import { AiAssistantMissingKeyComponent } from './components/ai-assistant-missing-key.component';
import { AiQuestionResponse } from './components/ai-assistant-question.component';
import { AiAssistantResizeHandleComponent } from './components/ai-assistant-resize-handle.component';
import { AiAssistantThinkingComponent } from './components/ai-assistant-thinking.component';
import { AiAssistantUsageComponent } from './components/ai-assistant-usage.component';

@Component({
  selector: 'app-ai-assistant-panel',
  host: { class: 'block h-full min-h-0' },
  imports: [
    LucideArrowDown,
    MessageScrollerDirective,
    MessageScrollerItemDirective,
    AiAssistantChangeSetComponent,
    AiAssistantComposerComponent,
    AiAssistantContextComponent,
    AiAssistantEmptyStateComponent,
    AiAssistantHeaderComponent,
    AiAssistantHistoryComponent,
    AiAssistantMessageComponent,
    AiAssistantMissingKeyComponent,
    AiAssistantResizeHandleComponent,
    AiAssistantThinkingComponent,
    AiAssistantUsageComponent,
  ],
  template: `
    <div
      class="text-foreground relative flex h-full w-full flex-col overflow-hidden"
      role="region"
      i18n-aria-label="Accessible name of the AI assistant panel"
      aria-label="AI assistant"
      [class.bg-card]="!isPage()">
      @if (isDrawer()) {
        <app-ai-assistant-resize-handle
          [width]="panel.width()"
          (widthChange)="panel.setWidth($event)"
          (resizingChange)="panel.setResizing($event)" />
      }

      <app-ai-assistant-header
        [title]="headerTitle()"
        [mode]="panel.mode()"
        [contentWidth]="contentWidth()"
        [closable]="isDrawer()"
        (historyToggled)="toggleHistory()"
        (modeChange)="setMode($event)"
        (newChat)="startNew()"
        (closed)="panel.close()" />

      <div class="relative flex min-h-0 flex-1 flex-col">
        <div
          appMessageScroller
          #scroller="messageScroller"
          class="custom-scroll flex min-h-0 flex-1 flex-col overflow-y-auto"
          role="log"
          i18n-aria-label="Accessible name of the assistant transcript"
          aria-label="Conversation">
          <div
            class="mx-auto flex w-full flex-1 flex-col px-4 py-4"
            [class]="contentWidth()">
            @if (isMissingKey()) {
              <app-ai-assistant-missing-key
                class="mb-4"
                [workspace]="assistant.workspaceKey()" />
            }

            @if (assistant.showHistory()) {
              <app-ai-assistant-history
                [conversations]="assistant.conversations()"
                (opened)="openConversation($event)"
                (deleted)="deleteConversation($event)" />
            } @else if (entries().length === 0) {
              <app-ai-assistant-empty-state (selected)="send($event)" />
            } @else {
              @if (assistant.droppedMessages() > 0) {
                <p
                  class="text-muted mb-4 text-center text-xs"
                  i18n="
                    Shown when older messages were left out of what the
                    assistant can see
                  ">
                  Older messages in this chat are no longer in the assistant's
                  context.
                </p>
              }

              <div class="flex flex-col gap-5">
                @for (entry of entries(); track $index) {
                  <app-ai-assistant-message
                    [appMessageScrollerItem]="'entry-' + $index"
                    [scrollAnchor]="entry.role === 'user'"
                    [entry]="entry"
                    [references]="assistant.references()"
                    [changeSets]="assistant.appliedChangeSets()"
                    [answers]="assistant.answers()"
                    [workspace]="assistant.workspaceKey()"
                    [isStreaming]="assistant.isStreaming() && $last"
                    [isLast]="$last && !assistant.isStreaming()"
                    (retried)="retry()"
                    (answered)="answer($event)"
                    (edited)="assistant.editLastQuestion()" />
                }

                @if (isThinking()) {
                  <app-ai-assistant-thinking
                    [elapsedMs]="assistant.turnElapsedMs()"
                    [usage]="assistant.turnUsage()" />
                }
              </div>
            }
          </div>
        </div>

        @if (!scroller.atEnd()) {
          <button
            type="button"
            class="bg-card border-border text-muted hover:text-foreground absolute inset-x-0 bottom-3 mx-auto flex h-8 w-8 items-center justify-center rounded-full border shadow-md transition-colors"
            i18n-aria-label="
              Accessible label for the button that scrolls to the newest message
            "
            aria-label="Jump to latest"
            (click)="scroller.scrollToEnd('smooth')">
            <svg lucideArrowDown class="h-4 w-4"></svg>
          </button>
        }
      </div>

      @if (assistant.changeSet(); as proposal) {
        <app-ai-assistant-change-set
          [changeSet]="proposal"
          [excludedChangeIds]="assistant.excludedChangeIds()"
          [isApplying]="assistant.isApplying()"
          [contentWidth]="contentWidth()"
          [workspace]="assistant.workspaceKey()"
          (toggled)="assistant.toggleChange($event)"
          (selectionChanged)="assistant.toggleChanges($event)"
          (applied)="apply()"
          (discarded)="discard()"
          (undone)="undo()"
          (stopped)="stopApplying()" />
      }

      @if (spend(); as usage) {
        <app-ai-assistant-usage
          [usage]="usage"
          [contentWidth]="contentWidth()" />
      }

      <app-ai-assistant-context
        [chips]="assistant.contextChips()"
        [hasRemoved]="assistant.hasRemovedContext()"
        [contentWidth]="contentWidth()"
        (removed)="assistant.removeContext($event)"
        (restored)="assistant.restoreContext()" />

      <app-ai-assistant-composer
        [disabled]="isMissingKey()"
        [models]="assistant.models()"
        [selectedModel]="assistant.selectedModel()"
        [modelLabel]="assistant.selectedModelLabel()"
        [efforts]="assistant.efforts"
        [selectedEffort]="assistant.selectedEffort()"
        [effortLabel]="assistant.selectedEffortLabel()"
        [supportsEffort]="assistant.supportsEffort()"
        [isStreaming]="assistant.isStreaming()"
        [isApplying]="assistant.isApplying()"
        [isAnswering]="assistant.pendingQuestion() !== null"
        [contentWidth]="contentWidth()"
        [draft]="assistant.draft()"
        (messageSent)="send($event)"
        [isReplacing]="assistant.isReplacingLastTurn()"
        (draftChanged)="assistant.setDraft($event)"
        (stopped)="assistant.stopTurn()"
        (editCancelled)="assistant.cancelEdit()"
        (modelSelected)="assistant.selectModel($event)"
        (effortSelected)="assistant.selectEffort($event)" />
    </div>
  `,
})
export class AiAssistantPanelComponent {
  readonly variant = input<'drawer' | 'page'>('drawer');

  private readonly scroller = viewChild.required(MessageScrollerDirective);
  private readonly injector = inject(Injector);

  private anchoredId: string | null = null;

  protected readonly assistant = inject(AiAssistantService);
  protected readonly panel = inject(AiPanelService);
  protected readonly entries = computed(() => this.assistant.entries());

  protected readonly isDrawer = computed(() => this.variant() === 'drawer');
  protected readonly isPage = computed(() => this.variant() === 'page');

  protected readonly isMissingKey = computed(() => {
    return this.assistant.hasCredentials() === false;
  });

  protected readonly spend = computed(() => {
    const usage = this.assistant.usage();

    if (usage === null || usage.outputTokens === 0) {
      return null;
    }

    return usage;
  });

  protected readonly isThinking = computed(() => {
    return this.assistant.isStreaming() && this.assistant.isThinking();
  });

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

  constructor() {
    const stopWatching = this.panel.watchTranscript();

    inject(DestroyRef).onDestroy(stopWatching);

    effect(() => {
      const entries = this.assistant.entries();
      const anchorId = this.latestTurnId(entries);

      if (anchorId === null || anchorId === this.anchoredId) {
        return;
      }

      this.anchoredId = anchorId;

      this.afterRender(() => this.scroller().anchorTurn(anchorId));
    });

    effect(() => {
      this.assistant.transcriptVersion();
      this.anchoredId = null;

      this.afterRender(() => this.scroller().scrollToEnd());
    });
  }

  private latestTurnId(entries: AiChatEntry[]): string | null {
    for (let index = entries.length - 1; index >= 0; index--) {
      if (entries[index].role === 'user') {
        return `entry-${index}`;
      }
    }

    return null;
  }

  private afterRender(action: () => void) {
    afterNextRender(action, { injector: this.injector });
  }

  protected apply() {
    void this.assistant.applyChangeSet();
  }

  protected discard() {
    void this.assistant.discardChangeSet();
  }

  protected stopApplying() {
    void this.assistant.stopApplying();
  }

  protected undo() {
    void this.assistant.undoChangeSet();
  }

  protected setMode(mode: AiDisplayMode) {
    this.panel.setMode(mode);
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
  }

  protected send(text: string) {
    void this.assistant.send(text, this.assistant.isReplacingLastTurn());
  }

  protected retry() {
    void this.assistant.retryLastTurn();
  }

  protected answer(response: AiQuestionResponse) {
    void this.assistant.answerQuestion(
      response.question,
      response.labels,
      response.text
    );
  }
}
