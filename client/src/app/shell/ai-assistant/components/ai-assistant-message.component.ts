import { Component, computed, input } from '@angular/core';
import { AiChatEntry } from '@core/services/ai-assistant.service';
import { LucideWrench } from '@lucide/angular';

@Component({
  selector: 'app-ai-assistant-message',
  host: {
    class: 'flex flex-col gap-1.5',
    '[class.items-end]': 'isUser()',
  },
  imports: [LucideWrench],
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
      <p
        class="text-sm leading-relaxed whitespace-pre-wrap"
        [class.text-error]="entry().failed">
        {{ entry().text }}
      </p>
    }
  `,
})
export class AiAssistantMessageComponent {
  readonly entry = input.required<AiChatEntry>();

  protected readonly isUser = computed(() => this.entry().role === 'user');
}
