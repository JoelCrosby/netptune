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
  host: { class: 'inline-flex' },
  imports: [TooltipDirective],
  template: `
    <span
      class="font-avatar text-muted hover:text-foreground cursor-default text-[10.5px] tabular-nums transition-colors"
      tabindex="0"
      [appTooltip]="breakdown()"
      appTooltipPosition="top">
      {{ label() }}
    </span>
  `,
})
export class AiAssistantUsageComponent {
  readonly usage = input.required<AiTokenUsage>();

  protected readonly label = computed(() => {
    const usage = this.usage();
    const tokens = formatTokens(usage);
    const cost = formatCost(usage);

    return $localize`:Assistant spend shown under the latest reply, for example "12.4k tok · $0.08":${tokens}:tokens: tok · ${cost}:cost:`;
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
