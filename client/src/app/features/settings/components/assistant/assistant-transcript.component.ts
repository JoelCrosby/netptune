import { Component, computed, input, output } from '@angular/core';
import { AiAssistantMessageComponent } from '@app/shell/ai-assistant/components/ai-assistant-message.component';
import { toChatEntries } from '@core/models/ai-chat-entry';
import { AiConversationDetail } from '@core/models/ai-conversation';
import { referenceMap } from '@core/util/ai-references';
import { formatCost } from '@core/util/ai-usage';
import { LucideArrowLeft } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';

@Component({
  selector: 'app-assistant-transcript',
  imports: [AiAssistantMessageComponent, IconButtonComponent, LucideArrowLeft],
  host: { class: 'block' },
  template: `
    <section
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
      <header class="border-border flex items-start gap-3 border-b px-6 py-5">
        <button
          app-icon-button
          class="mt-0.5 h-8 w-8 shrink-0"
          type="button"
          i18n-aria-label="
            Accessible label for the button that leaves a conversation
          "
          aria-label="Back to conversations"
          (click)="closed.emit()">
          <svg lucideArrowLeft class="h-4 w-4"></svg>
        </button>

        <div class="min-w-0">
          <h2 class="font-overpass truncate text-base font-semibold">
            {{ detail().conversation.title }}
          </h2>
          <p class="text-muted mt-1 text-xs">
            @if (member(); as name) {
              {{ name }} ·
            }
            {{ detail().conversation.model }} ·
            {{ detail().conversation.usage.inputTokens }}
            <span i18n="Counts tokens sent to the model">in</span> ·
            {{ detail().conversation.usage.outputTokens }}
            <span i18n="Counts tokens returned by the model">out</span> ·
            {{ detail().conversation.usage.cacheReadTokens }}
            <span i18n="Counts tokens read from the provider prompt cache"
              >cached</span
            >
            ·
            {{ detail().conversation.usage.cacheCreationTokens }}
            <span i18n="Counts tokens written to the provider prompt cache"
              >written</span
            >
            · {{ costLabel() }}
          </p>
        </div>
      </header>

      <div class="flex flex-col gap-5 px-6 py-5">
        @for (entry of entries(); track $index) {
          <app-ai-assistant-message
            [entry]="entry"
            [references]="references()"
            [workspace]="workspace()" />
        }
      </div>
    </section>
  `,
})
export class AssistantTranscriptComponent {
  readonly detail = input.required<AiConversationDetail>();
  readonly member = input.required<string | null>();
  readonly workspace = input.required<string | null>();

  readonly closed = output();

  protected readonly costLabel = computed(() => {
    return formatCost(this.detail().conversation.usage);
  });

  protected readonly entries = computed(() => {
    return toChatEntries(this.detail().messages);
  });

  protected readonly references = computed(() => {
    const messages = this.detail().messages;

    return referenceMap(messages.flatMap((message) => message.references));
  });
}
