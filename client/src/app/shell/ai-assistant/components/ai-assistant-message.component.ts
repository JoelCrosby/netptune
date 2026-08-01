import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiEntityReference } from '@core/models/ai-conversation';
import { AiChatEntry } from '@core/services/ai-assistant.service';
import {
  parseAssistantText,
  referenceKey,
  referenceRoute,
} from '@core/util/ai-references';
import { LucideWrench } from '@lucide/angular';

interface RenderedSegment {
  text: string;
  link: string[] | null;
}

@Component({
  selector: 'app-ai-assistant-message',
  host: {
    class: 'flex flex-col gap-1.5',
    '[class.items-end]': 'isUser()',
  },
  imports: [LucideWrench, RouterLink],
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
        @for (segment of segments(); track $index) {
          @if (segment.link) {
            <a
              [routerLink]="segment.link"
              class="bg-primary/10 text-primary rounded px-1 py-0.5 font-medium hover:underline"
              >{{ segment.text }}</a
            >
          } @else {
            <span>{{ segment.text }}</span>
          }
        }
      </p>
    }
  `,
})
export class AiAssistantMessageComponent {
  readonly entry = input.required<AiChatEntry>();
  readonly references = input<Map<string, AiEntityReference>>(new Map());
  readonly workspace = input<string | null>(null);
  readonly isStreaming = input(false);

  protected readonly isUser = computed(() => this.entry().role === 'user');

  protected readonly segments = computed<RenderedSegment[]>(() => {
    const parsed = parseAssistantText(this.entry().text, this.isStreaming());

    return parsed.map((segment) => {
      if (segment.kind === 'text') {
        return { text: segment.value, link: null };
      }

      return {
        text: segment.label,
        link: this.linkFor(segment.type, segment.id),
      };
    });
  });

  private linkFor(type: string, id: string): string[] | null {
    const workspace = this.workspace();
    const isKnown = this.references().has(referenceKey(type, id));

    if (!workspace || !isKnown) {
      return null;
    }

    return referenceRoute(workspace, type, id);
  }
}
