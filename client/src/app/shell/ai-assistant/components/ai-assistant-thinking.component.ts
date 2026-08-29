import { Component, computed, input } from '@angular/core';
import { AiTokenUsage } from '@core/models/ai-conversation';
import { formatTokens, totalTokens } from '@core/util/ai-usage';
import { formatElapsed } from '@core/util/duration';

/** A clock reading "0s" says nothing, so it appears once there is a second to show. */
const ELAPSED_FLOOR = 1000;

@Component({
  selector: 'app-ai-assistant-thinking',
  host: { class: 'text-muted flex items-center gap-2 text-sm' },
  template: `
    <svg
      class="h-4 w-4 shrink-0 [stroke-linecap:round]"
      viewBox="13.25 13.25 37.5 37.5"
      fill="none"
      stroke-width="2.5"
      aria-hidden="true">
      <circle class="stroke-current" cx="32" cy="32" r="17.5"></circle>
      <path class="stroke-primary" d="M14.5 32h35"></path>
      <ellipse
        class="stroke-primary animate-globe-meridian origin-center motion-reduce:animate-none"
        cx="32"
        cy="32"
        rx="8.75"
        ry="17.5"></ellipse>
    </svg>

    <span
      class="after:animate-thinking-dots after:inline-block after:content-['...'] motion-reduce:after:animate-none"
      i18n="Shown while the assistant is preparing its reply"
      >Thinking</span
    >

    @if (progress(); as progress) {
      <span class="text-muted text-xs tabular-nums">{{ progress }}</span>
    }
  `,
})
export class AiAssistantThinkingComponent {
  readonly elapsedMs = input(0);
  readonly usage = input<AiTokenUsage | null>(null);

  /**
   * A count that keeps moving is what says the turn is alive. Tokens only land
   * as the model finishes each call, so the clock carries the wait until then.
   */
  protected readonly progress = computed(() => {
    const elapsedMs = this.elapsedMs();
    const hasStarted = elapsedMs >= ELAPSED_FLOOR;

    if (!hasStarted) {
      return null;
    }

    const elapsed = formatElapsed(elapsedMs);
    const usage = this.usage() ?? undefined;
    const hasTokens = totalTokens(usage) > 0;

    if (!hasTokens) {
      return elapsed;
    }

    const tokens = formatTokens(usage);

    return $localize`:Assistant progress while a reply is being prepared, for example "8s · 12.4k tokens":${elapsed}:elapsed: · ${tokens}:tokens: tokens`;
  });
}
