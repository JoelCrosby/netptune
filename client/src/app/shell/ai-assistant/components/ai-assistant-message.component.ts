import { Component, computed, input } from '@angular/core';
import { AiEntityReference } from '@core/models/ai-conversation';
import { AiChatEntry } from '@core/services/ai-assistant.service';
import { parseAssistantMarkdown } from '@core/util/ai-markdown';
import { LucideWrench } from '@lucide/angular';
import { AiAssistantMarkdownComponent } from './ai-assistant-markdown.component';

@Component({
  selector: 'app-ai-assistant-message',
  host: {
    class: 'flex flex-col gap-1.5',
    '[class.items-end]': 'isUser()',
  },
  imports: [LucideWrench, AiAssistantMarkdownComponent],
  template: `
    @if (entry().tools.length > 0) {
      <div class="text-muted flex flex-wrap items-center gap-1.5 text-xs">
        <svg lucideWrench class="h-3 w-3"></svg>
        @for (tool of entry().tools; track $index) {
          <span
            class="bg-hover rounded px-1.5 py-0.5 font-mono text-[0.7rem]"
            >{{ tool }}</span
          >
        }
      </div>
    }

    @if (isUser()) {
      <p
        class="bg-hover max-w-[85%] rounded-2xl px-4 py-2.5 text-sm whitespace-pre-wrap">
        {{ entry().text }}
      </p>
    } @else {
      <app-ai-assistant-markdown
        [class.text-error]="entry().failed"
        [blocks]="blocks()"
        [references]="references()"
        [workspace]="workspace()" />
    }
  `,
})
export class AiAssistantMessageComponent {
  readonly entry = input.required<AiChatEntry>();
  readonly references = input<Map<string, AiEntityReference>>(new Map());
  readonly workspace = input<string | null>(null);
  readonly isStreaming = input(false);

  protected readonly isUser = computed(() => this.entry().role === 'user');

  protected readonly blocks = computed(() => {
    return parseAssistantMarkdown(this.entry().text, this.isStreaming());
  });
}
