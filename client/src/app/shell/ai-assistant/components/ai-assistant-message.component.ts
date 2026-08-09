import { Component, computed, input, output } from '@angular/core';
import { AiEntityReference } from '@core/models/ai-conversation';
import { AiChatEntry } from '@core/services/ai-assistant.service';
import { parseAssistantMarkdown } from '@core/util/ai-markdown';
import { formatElapsed } from '@core/util/duration';
import {
  LucideBrain,
  LucidePencil,
  LucideRefreshCw,
  LucideWrench,
} from '@lucide/angular';
import { AiAssistantMarkdownComponent } from './ai-assistant-markdown.component';

interface AiToolChip {
  name: string;
  count: number;
}

/** Below a second there is nothing worth reporting. */
const MINIMUM_REPORTED_DURATION = 1000;

@Component({
  selector: 'app-ai-assistant-message',
  host: {
    class: 'flex flex-col gap-1.5',
    '[class.items-end]': 'isUser()',
  },
  imports: [
    LucideBrain,
    LucidePencil,
    LucideRefreshCw,
    LucideWrench,
    AiAssistantMarkdownComponent,
  ],
  template: `
    @if (thoughtFor(); as duration) {
      <p class="text-muted flex items-center gap-1 text-xs">
        <svg lucideBrain class="h-3 w-3"></svg>
        {{ duration }}
      </p>
    }

    @if (tools().length > 0) {
      <div class="text-muted flex flex-wrap items-center gap-1.5 text-xs">
        <svg lucideWrench class="h-3 w-3"></svg>
        @for (tool of tools(); track tool.name) {
          <span
            class="bg-hover flex items-center gap-1 rounded px-1.5 py-0.5 font-mono text-[0.7rem]">
            {{ tool.name }}
            @if (tool.count > 1) {
              <span class="text-muted/80">×{{ tool.count }}</span>
            }
          </span>
        }
      </div>
    }

    @if (isUser()) {
      <p
        class="bg-hover max-w-[85%] rounded-2xl px-4 py-2.5 text-sm whitespace-pre-wrap">
        {{ entry().text }}
      </p>

      @if (isLast()) {
        <button
          type="button"
          class="text-muted hover:text-foreground flex items-center gap-1 text-xs"
          (click)="edited.emit()">
          <svg lucidePencil class="h-3 w-3"></svg>
          <span i18n="Button that reopens the last question for rewording">
            Edit
          </span>
        </button>
      }
    } @else {
      <app-ai-assistant-markdown
        [class.text-error]="entry().failed"
        [blocks]="blocks()"
        [references]="references()"
        [workspace]="workspace()" />

      @if (entry().stopped) {
        <p
          class="text-muted mt-1 text-xs italic"
          i18n="Shown under a reply the user stopped">
          You stopped this reply.
        </p>
      }

      @if (isRetryable()) {
        <button
          type="button"
          class="text-muted hover:text-foreground mt-1 flex items-center gap-1 text-xs"
          (click)="retried.emit()">
          <svg lucideRefreshCw class="h-3 w-3"></svg>
          @if (entry().failed) {
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
    }
  `,
})
export class AiAssistantMessageComponent {
  readonly entry = input.required<AiChatEntry>();
  readonly references = input<Map<string, AiEntityReference>>(new Map());
  readonly workspace = input<string | null>(null);
  readonly isStreaming = input(false);
  readonly isLast = input(false);

  readonly retried = output();
  readonly edited = output();

  protected readonly isUser = computed(() => this.entry().role === 'user');

  /** A reply that never arrived has no work to report, however long it took. */
  protected readonly thoughtFor = computed(() => {
    const entry = this.entry();
    const durationMs = entry.durationMs ?? 0;
    const isWorthReporting =
      !this.isUser() &&
      !entry.failed &&
      !this.isStreaming() &&
      durationMs >= MINIMUM_REPORTED_DURATION;

    if (!isWorthReporting) {
      return null;
    }

    const duration = formatElapsed(durationMs);

    return $localize`:Shown above a finished reply, saying how long the assistant worked on it:Thought for ${duration}:duration:`;
  });

  protected readonly isRetryable = computed(() => {
    return this.isLast() && !this.isUser() && !this.isStreaming();
  });

  protected readonly tools = computed<AiToolChip[]>(() => {
    const counts = new Map<string, number>();

    for (const tool of this.entry().tools) {
      counts.set(tool, (counts.get(tool) ?? 0) + 1);
    }

    return [...counts].map(([name, count]) => ({ name, count }));
  });

  protected readonly blocks = computed(() => {
    return parseAssistantMarkdown(this.entry().text, this.isStreaming());
  });
}
