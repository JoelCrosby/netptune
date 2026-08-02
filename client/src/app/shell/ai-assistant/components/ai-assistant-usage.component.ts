import { Component, computed, input } from '@angular/core';
import { AiTokenUsage } from '@core/models/ai-conversation';
import {
  formatCost,
  formatTokenCount,
  formatTokens,
} from '@core/util/ai-usage';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-ai-assistant-usage',
  host: { class: 'block' },
  imports: [TooltipDirective],
  template: `
    <div class="mx-auto flex w-full justify-end px-5" [class]="contentWidth()">
      <span
        class="text-muted hover:text-foreground cursor-default text-xs transition-colors"
        tabindex="0"
        [appTooltip]="breakdown()"
        appTooltipPosition="top">
        {{ label() }}
      </span>
    </div>
  `,
})
export class AiAssistantUsageComponent {
  readonly usage = input.required<AiTokenUsage>();
  readonly contentWidth = input('');

  protected readonly label = computed(() => {
    const usage = this.usage();
    const tokens = formatTokens(usage);
    const cost = formatCost(usage);

    return $localize`:Assistant spend shown above the message box, for example "12.4k tokens · $0.08":${tokens}:tokens: tokens · ${cost}:cost:`;
  });

  protected readonly breakdown = computed(() => {
    const usage = this.usage();
    const sent = formatTokenCount(usage.inputTokens);
    const received = formatTokenCount(usage.outputTokens);
    const cacheRead = formatTokenCount(usage.cacheReadTokens);
    const cacheWritten = formatTokenCount(usage.cacheCreationTokens);

    return $localize`:Tooltip breaking assistant token spend down by category:${sent}:sent: sent · ${received}:received: received · ${cacheRead}:cacheRead: cached · ${cacheWritten}:cacheWritten: written`;
  });
}
