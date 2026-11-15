import { NgTemplateOutlet } from '@angular/common';
import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
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
import { AiAssistantEmptyStateComponent } from './components/ai-assistant-empty-state.component';
import { AiAssistantGreetingComponent } from './components/ai-assistant-greeting.component';
import { AiAssistantHeaderComponent } from './components/ai-assistant-header.component';
import { AiAssistantPromptsComponent } from './components/ai-assistant-prompts.component';
import { AiAssistantMessageComponent } from './components/ai-assistant-message.component';
import { AiAssistantMissingKeyComponent } from './components/ai-assistant-missing-key.component';
import { AiQuestionResponse } from './components/ai-assistant-question.component';
import {
  MAX_AI_PANEL_WIDTH,
  MIN_AI_PANEL_WIDTH,
} from '@core/models/ai-panel-width';
import { ResizeHandleComponent } from '@static/components/resize-handle.component';

@Component({
  selector: 'app-ai-assistant-panel',
  host: {
    class: 'block h-full min-h-0',
    '(keydown.escape)': 'stopOnEscape($event)',
  },
  imports: [
    LucideArrowDown,
    NgTemplateOutlet,
    MessageScrollerDirective,
    MessageScrollerItemDirective,
    AiAssistantChangeSetComponent,
    AiAssistantComposerComponent,
    AiAssistantEmptyStateComponent,
    AiAssistantGreetingComponent,
    AiAssistantHeaderComponent,
    AiAssistantMessageComponent,
    AiAssistantMissingKeyComponent,
    AiAssistantPromptsComponent,
    ResizeHandleComponent,
  ],
  template: `
    <div
      class="text-foreground relative flex h-full w-full flex-col overflow-hidden"
      role="region"
      i18n-aria-label="Accessible name of the AI assistant panel"
      aria-label="AI assistant"
      [class.bg-card]="!isPage()">
      @if (isDrawer()) {
        <app-resize-handle
          i18n-label="
            Accessible name of the handle that resizes the assistant panel
          "
          label="Resize assistant panel"
          [width]="panel.width()"
          [minWidth]="minPanelWidth"
          [maxWidth]="maxPanelWidth"
          (widthChange)="panel.setWidth($event)"
          (resizingChange)="panel.setResizing($event)" />
      }

      <app-ai-assistant-header
        [style.paddingRight.px]="scrollbarGutter()"
        [title]="headerTitle()"
        [subtitle]="headerSubtitle()"
        [mode]="panel.mode()"
        [contentWidth]="contentWidth()"
        [roomy]="isPage()"
        [closable]="!isPage()"
        [hasModeMenu]="!isReview()"
        (modeChange)="setMode($event)"
        (newChat)="startNew()"
        (closed)="close()" />

      <div class="relative flex min-h-0 flex-1 flex-col">
        <div
          appMessageScroller
          #scroller="messageScroller"
          #scrollPane
          class="custom-scroll scroll-gutter flex min-h-0 flex-1 flex-col overflow-y-auto"
          role="log"
          i18n-aria-label="Accessible name of the assistant transcript"
          aria-label="Conversation">
          <div
            class="mx-auto flex w-full flex-1 flex-col"
            [class]="transcriptClass()">
            @if (isMissingKey()) {
              <app-ai-assistant-missing-key
                class="mb-4"
                [workspace]="assistant.workspaceKey()" />
            }

            @if (entries().length === 0) {
              @if (isCenteredStart()) {
                <div
                  class="flex flex-1 flex-col items-center justify-center gap-7 pb-14">
                  <app-ai-assistant-greeting [roomy]="true" />

                  <div class="w-full">
                    <ng-container *ngTemplateOutlet="composer" />
                  </div>

                  <app-ai-assistant-prompts
                    [roomy]="true"
                    (selected)="send($event)" />
                </div>
              } @else {
                <app-ai-assistant-empty-state (selected)="send($event)" />
              }
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

              <div
                class="flex flex-col"
                [class]="isPage() ? 'gap-6' : 'gap-4.5'">
                @for (entry of entries(); track $index) {
                  <app-ai-assistant-message
                    [appMessageScrollerItem]="'entry-' + $index"
                    [scrollAnchor]="entry.role === 'user'"
                    [class.mt-4]="isPage() && entry.role === 'user' && !$first"
                    [roomy]="isPage()"
                    [entry]="entry"
                    [references]="assistant.references()"
                    [changeSets]="assistant.appliedChangeSets()"
                    [answers]="assistant.answers()"
                    [workspace]="assistant.workspaceKey()"
                    [isStreaming]="assistant.isStreaming() && $last"
                    [isThinking]="assistant.isThinking()"
                    [elapsedMs]="assistant.turnElapsedMs()"
                    [turnUsage]="assistant.turnUsage()"
                    [usage]="$last ? spend() : null"
                    [isLast]="$last && !assistant.isStreaming()"
                    (retried)="retry()"
                    (answered)="answer($event)"
                    (edited)="assistant.editLastQuestion()" />
                }
              </div>
            }
          </div>
        </div>

        @if (!scroller.atEnd()) {
          <button
            type="button"
            class="bg-card border-border text-muted hover:text-foreground absolute inset-x-0 bottom-3 mx-auto flex h-8 w-8 items-center justify-center rounded-full border shadow-md transition-colors"
            [style.right.px]="scrollbarGutter()"
            i18n-aria-label="
              Accessible label for the button that scrolls to the newest message
            "
            aria-label="Jump to latest"
            (click)="scroller.scrollToEnd('smooth')">
            <svg lucideArrowDown class="h-4 w-4"></svg>
          </button>
        }
      </div>

      <!-- Matches the transcript's scrollbar gutter so both columns centre on
           the same line. -->
      <div [style.paddingRight.px]="scrollbarGutter()">
        @if (!isReview() && assistant.changeSet(); as proposal) {
          <app-ai-assistant-change-set
            [changeSet]="proposal"
            [excludedChangeIds]="assistant.excludedChangeIds()"
            [isApplying]="assistant.isApplying()"
            [contentWidth]="contentWidth()"
            [roomy]="isPage()"
            [workspace]="assistant.workspaceKey()"
            (toggled)="assistant.toggleChange($event)"
            (selectionChanged)="assistant.toggleChanges($event)"
            (applied)="apply()"
            (discarded)="discard()"
            (undone)="undo()"
            (stopped)="stopApplying()" />
        }

        @if (!isCenteredStart()) {
          <ng-container *ngTemplateOutlet="composer" />
        }
      </div>
    </div>

    <ng-template #composer>
      <app-ai-assistant-composer
        [roomy]="isPage()"
        [inline]="isCenteredStart()"
        [disabled]="isMissingKey()"
        [models]="assistant.models()"
        [selectedModel]="assistant.selectedModel()"
        [modelLabel]="assistant.selectedModelLabel()"
        [efforts]="assistant.efforts()"
        [selectedEffort]="assistant.effectiveEffort()"
        [effortLabel]="assistant.selectedEffortLabel()"
        [supportsEffort]="assistant.supportsEffort()"
        [isStreaming]="assistant.isStreaming()"
        [isApplying]="assistant.isApplying()"
        [isAnswering]="assistant.pendingQuestion() !== null"
        [contentWidth]="contentWidth()"
        [draft]="assistant.draft()"
        [chips]="assistant.contextChips()"
        [hasRemovedContext]="assistant.hasRemovedContext()"
        (contextRemoved)="assistant.removeContext($event)"
        (contextRestored)="assistant.restoreContext()"
        (messageSent)="send($event)"
        [isReplacing]="assistant.isReplacingLastTurn()"
        (draftChanged)="assistant.setDraft($event)"
        (stopped)="assistant.stopTurn()"
        (editCancelled)="assistant.cancelEdit()"
        (modelSelected)="assistant.selectModel($event)"
        (effortSelected)="assistant.selectEffort($event)" />
    </ng-template>
  `,
})
export class AiAssistantPanelComponent {
  // The review variant docks inside the review dialog, which is itself the
  // change set's surface, so the proposal card and display modes stay out.
  readonly variant = input<'drawer' | 'page' | 'review'>('drawer');

  readonly closed = output();

  private readonly scroller = viewChild.required(MessageScrollerDirective);
  private readonly composer = viewChild.required(AiAssistantComposerComponent);
  private readonly scrollPane =
    viewChild.required<ElementRef<HTMLElement>>('scrollPane');
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  private anchoredId: string | null = null;

  // The scroll pane holds its gutter whether or not the scrollbar shows, so one
  // reading stands for the life of the panel. Platforms that overlay their
  // scrollbars report nothing, which is the right answer there.
  protected readonly scrollbarGutter = signal(0);

  protected readonly assistant = inject(AiAssistantService);
  protected readonly panel = inject(AiPanelService);
  protected readonly minPanelWidth = MIN_AI_PANEL_WIDTH;
  protected readonly maxPanelWidth = MAX_AI_PANEL_WIDTH;
  protected readonly entries = computed(() => this.assistant.entries());

  protected readonly isDrawer = computed(() => this.variant() === 'drawer');
  protected readonly isPage = computed(() => this.variant() === 'page');
  protected readonly isReview = computed(() => this.variant() === 'review');

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

  protected readonly headerSubtitle = computed(() => {
    const names = this.assistant
      .contextChips()
      .filter((chip) => chip.kind !== 'view')
      .map((chip) => chip.name);

    return names.length > 0 ? names.join(' · ') : null;
  });

  protected readonly contentWidth = computed(() => {
    return this.isPage() ? 'max-w-3xl' : '';
  });

  protected readonly transcriptClass = computed(() => {
    const width = this.contentWidth();

    return this.isPage()
      ? `${width} px-6 pt-8 pb-6`
      : `${width} px-4.5 pt-5 pb-3`;
  });

  // A new chat on the page puts the greeting and the composer together in the
  // middle, the way a blank slate reads best; once a reply lands the composer
  // docks at the bottom.
  protected readonly isCenteredStart = computed(() => {
    const isEmpty = this.entries().length === 0;

    return this.isPage() && isEmpty && !this.isMissingKey();
  });

  protected readonly headerTitle = computed(() => {
    const title = this.assistant.conversationTitle();

    if (title) {
      return title;
    }

    // The greeting already says this is a blank slate, so the page leaves the
    // header to the actions alone.
    if (this.isCenteredStart()) {
      return '';
    }

    return $localize`:Header of a conversation that has not started yet:New chat`;
  });

  constructor() {
    const stopWatching = this.panel.watchTranscript();

    this.destroyRef.onDestroy(stopWatching);

    this.afterRender(() => this.watchScrollbarGutter());

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

  // The gutter only moves when the pane itself does — a zoom, or a platform
  // that swaps its scrollbars — so it is read off resizes rather than polled.
  private watchScrollbarGutter() {
    const pane = this.scrollPane().nativeElement;

    const measure = () => {
      this.scrollbarGutter.set(pane.offsetWidth - pane.clientWidth);
    };

    measure();

    const observer = new ResizeObserver(measure);

    observer.observe(pane);

    this.destroyRef.onDestroy(() => observer.disconnect());
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

  focusComposer() {
    this.afterRender(() => this.composer().focus());
  }

  // Escape that stops a reply, or closes the panel floating over a dialog, stops
  // there so it does not also close the dialog.
  protected stopOnEscape(event: Event) {
    const isStreaming = this.assistant.isStreaming();

    if (isStreaming) {
      event.stopPropagation();
      this.assistant.stopTurn();

      return;
    }

    const isOverDialog = this.isDrawer() && this.panel.isOverDialog();

    if (isOverDialog) {
      event.stopPropagation();
      this.panel.close();
    }
  }

  protected close() {
    if (this.isDrawer()) {
      this.panel.close();
    }

    this.closed.emit();
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
