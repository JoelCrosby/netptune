import { Component, computed, input } from '@angular/core';
import { AiTokenUsage } from '@core/models/ai-conversation';
import { formatTokenCount, formatUsage } from '@core/util/ai-usage';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-token-usage',
  host: { class: 'inline-flex' },
  imports: [TooltipDirective],
  template: `
    <span
      class="text-muted hover:text-foreground cursor-default tabular-nums transition-colors"
      [class]="sizeClass()"
      tabindex="0"
      [appTooltip]="breakdown()"
      appTooltipPosition="top">
      {{ label() }}
    </span>
  `,
})
export class TokenUsageComponent {
  readonly usage = input.required<AiTokenUsage>();
  readonly size = input<'small' | 'inherit'>('inherit');

  protected readonly sizeClass = computed(() => {
    return this.size() === 'small' ? 'font-avatar text-[10.5px]' : '';
  });

  protected readonly label = computed(() => formatUsage(this.usage()));

  protected readonly breakdown = computed(() => {
    const usage = this.usage();
    const sent = formatTokenCount(usage.inputTokens);
    const received = formatTokenCount(usage.outputTokens);
    const cacheRead = formatTokenCount(usage.cacheReadTokens);
    const cacheWritten = formatTokenCount(usage.cacheCreationTokens);

    return $localize`:Tooltip breaking assistant token spend down by category:${sent}:sent: sent · ${received}:received: received · ${cacheRead}:cacheRead: cached · ${cacheWritten}:cacheWritten: written`;
  });
}
