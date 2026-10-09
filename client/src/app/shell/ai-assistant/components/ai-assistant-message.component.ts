import {
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import {
  AiChangeApplyStatus,
  AiChangeSet,
  AiEntityReference,
  AiQuestionAnswer,
  AiTokenUsage,
} from '@core/models/ai-conversation';
import { AiChatEntry } from '@core/models/ai-chat-entry';
import { DialogService } from '@core/services/dialog.service';
import { parseAssistantMarkdown } from '@core/util/ai-markdown';
import {
  LucideCheck,
  LucideCopy,
  LucideListChecks,
  LucidePencil,
  LucideRefreshCw,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { AiAssistantMarkdownComponent } from './ai-assistant-markdown.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import {
  AiAssistantQuestionComponent,
  AiQuestionResponse,
} from './ai-assistant-question.component';
import {
  AiAssistantReviewDialogComponent,
  AiReviewData,
} from './ai-assistant-review-dialog.component';
import { AiAssistantUsageComponent } from './ai-assistant-usage.component';
import { AiAssistantWorkLogComponent } from './ai-assistant-work-log.component';

const COPIED_RESET_MS = 1500;

@Component({
  selector: 'app-ai-assistant-message',
  host: {
    class: 'flex flex-col gap-3',
    '[class.items-end]': 'isUser()',
  },
  imports: [
    AiAssistantMarkdownComponent,
    AiAssistantQuestionComponent,
    AiAssistantUsageComponent,
    AiAssistantWorkLogComponent,
    IconButtonComponent,
    InlineButtonComponent,
    LucideCheck,
    LucideCopy,
    LucideListChecks,
    LucidePencil,
    LucideRefreshCw,
    TooltipDirective,
  ],
  template: `
    @if (isUser()) {
      @if (entry().answer; as answer) {
        <p
          class="bg-primary/10 border-primary/25 max-w-[85%] rounded-[14px_14px_4px_14px] border px-3.5 py-2.25 text-[13.5px] leading-normal whitespace-pre-wrap">
          {{ describeAnswer(answer) }}
        </p>
      } @else if (changeSet(); as applied) {
        <button
          type="button"
          class="bg-primary/10 hover:border-primary/35 flex max-w-[85%] items-center gap-2.5 rounded-[14px_14px_4px_14px] border border-transparent px-3.5 py-2.25 text-left text-[13.5px] transition-colors"
          (click)="showChanges(applied)">
          <svg lucideListChecks class="text-primary h-4 w-4 shrink-0"></svg>
          <span>{{ changeSummary() }}</span>
          <span
            class="text-muted text-xs"
            i18n="Button that opens the table of changes the assistant made">
            View changes
          </span>
        </button>
      } @else {
        <p
          class="bg-primary/10 max-w-[85%] rounded-[14px_14px_4px_14px] px-3.5 py-2.25 text-[13.5px] leading-normal whitespace-pre-wrap">
          {{ entry().text }}
        </p>
      }

      @if (isLast() && !changeSet() && !entry().answer) {
        <button
          type="button"
          app-inline-button
          color="muted"
          appearance="lift"
          class="gap-1"
          (click)="edited.emit()">
          <svg lucidePencil class="h-3 w-3"></svg>
          <span i18n="Button that reopens the last question for rewording">
            Edit
          </span>
        </button>
      }
    } @else {
      <app-ai-assistant-work-log
        [entry]="entry()"
        [isLive]="isStreaming()"
        [isThinking]="isThinking()"
        [elapsedMs]="elapsedMs()"
        [usage]="turnUsage()" />

      <app-ai-assistant-markdown
        [class.text-error]="entry().failed"
        [blocks]="blocks()"
        [references]="references()"
        [workspace]="workspace()" />

      @if (entry().question; as question) {
        <app-ai-assistant-question
          class="mt-1.5"
          [question]="question"
          [answer]="answers().get(question.id) ?? null"
          [isActive]="isLast()"
          (answered)="answered.emit($event)" />
      }

      @if (hasFooter()) {
        <div class="text-muted -mt-1 flex items-center gap-0.5">
          @if (entry().stopped) {
            <span
              class="mr-2 text-xs italic"
              i18n="Shown under a reply the user stopped">
              You stopped this reply.
            </span>
          }

          @if (canCopy()) {
            <button
              app-icon-button
              type="button"
              color="muted"
              size="small"
              class="h-7 w-7 rounded-md"
              [ariaLabel]="copyLabel()"
              [appTooltip]="copyLabel()"
              appTooltipPosition="bottom"
              (click)="copy()">
              @if (isCopied()) {
                <svg lucideCheck class="h-3.5 w-3.5"></svg>
              } @else {
                <svg lucideCopy class="h-3.5 w-3.5"></svg>
              }
            </button>
          }

          @if (isRetryable()) {
            <button
              type="button"
              class="hover:bg-hover hover:text-foreground flex h-7 items-center gap-1.5 rounded-md px-2 text-xs transition-colors"
              (click)="retried.emit()">
              <svg lucideRefreshCw class="h-3.25 w-3.25"></svg>
              @if (entry().failed || entry().stopped) {
                <span i18n="Button that runs a failed assistant turn again">
                  Try again
                </span>
              } @else {
                <span i18n="Button that asks the assistant to answer again">
                  Regenerate
                </span>
              }
            </button>
          }

          <span class="flex-1"></span>

          @if (usage(); as usage) {
            <app-ai-assistant-usage [usage]="usage" />
          }
        </div>
      }
    }
  `,
})
export class AiAssistantMessageComponent {
  readonly entry = input.required<AiChatEntry>();
  readonly references = input<Map<string, AiEntityReference>>(new Map());
  readonly changeSets = input<Map<string, AiChangeSet>>(new Map());
  readonly answers = input<Map<string, AiQuestionAnswer>>(new Map());
  readonly workspace = input<string | null>(null);
  readonly isStreaming = input(false);
  readonly isThinking = input(false);
  readonly elapsedMs = input(0);
  readonly turnUsage = input<AiTokenUsage | null>(null);
  readonly usage = input<AiTokenUsage | null>(null);
  readonly isLast = input(false);

  readonly retried = output();
  readonly edited = output();
  readonly answered = output<AiQuestionResponse>();

  private readonly dialog = inject(DialogService);

  private copiedTimer: ReturnType<typeof setTimeout> | null = null;

  protected readonly isCopied = signal(false);

  protected readonly isUser = computed(() => this.entry().role === 'user');

  protected describeAnswer(answer: AiQuestionAnswer): string {
    return answer.text ?? answer.selectedLabels.join(', ');
  }

  protected readonly changeSet = computed(() => {
    const changeSetId = this.entry().changeSetId;

    if (changeSetId === undefined) {
      return null;
    }

    return this.changeSets().get(changeSetId) ?? null;
  });

  protected readonly changeSummary = computed(() => {
    const changeSet = this.changeSet();

    if (changeSet === null) {
      return '';
    }

    const total = changeSet.changes.length;
    const isUndone = !!changeSet.undoneAt;

    if (isUndone) {
      return $localize`:Describes a change set that was taken back:${total}:TOTAL: changes undone`;
    }

    const applied = changeSet.changes.filter((change) => {
      return change.applyStatus === AiChangeApplyStatus.applied;
    }).length;

    return $localize`:Describes what applying a change set did:${applied}:APPLIED: of ${total}:TOTAL: changes applied`;
  });

  protected showChanges(changeSet: AiChangeSet) {
    const data: AiReviewData = {
      changeSet,
      workspace: this.workspace(),
    };

    this.dialog.open<unknown, AiReviewData>(AiAssistantReviewDialogComponent, {
      data,
      width: '100vw',
      maxWidth: '100vw',
      height: '100vh',
      panelClass: 'np-review-dialog',
    });
  }

  protected readonly isRetryable = computed(() => {
    return this.isLast() && !this.isUser() && !this.isStreaming();
  });

  protected readonly canCopy = computed(() => {
    const entry = this.entry();
    const hasText = entry.text.trim().length > 0;

    return hasText && !entry.failed && !this.isStreaming();
  });

  protected readonly hasFooter = computed(() => {
    const hasUsage = this.usage() !== null;

    return (
      this.canCopy() ||
      this.isRetryable() ||
      hasUsage ||
      (!!this.entry().stopped && !this.isStreaming())
    );
  });

  protected readonly copyLabel = computed(() => {
    if (this.isCopied()) {
      return $localize`:Confirms the reply was copied to the clipboard:Copied`;
    }

    return $localize`:Tooltip on the button that copies a reply:Copy`;
  });

  protected async copy() {
    try {
      await navigator.clipboard.writeText(this.entry().text);
    } catch {
      return;
    }

    this.isCopied.set(true);

    if (this.copiedTimer !== null) {
      clearTimeout(this.copiedTimer);
    }

    this.copiedTimer = setTimeout(() => {
      this.isCopied.set(false);
      this.copiedTimer = null;
    }, COPIED_RESET_MS);
  }

  protected readonly blocks = computed(() => {
    return parseAssistantMarkdown(this.entry().text, this.isStreaming());
  });
}
