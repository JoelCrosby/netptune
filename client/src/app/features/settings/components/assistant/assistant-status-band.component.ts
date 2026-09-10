import { Component, computed, input } from '@angular/core';
import { AiSpend } from '@core/models/ai-spend';
import { formatCurrency } from '@core/util/ai-usage';
import { dateTimeFormat } from '@core/util/locale';
import { PanelComponent } from '@static/components/panel.component';
import {
  LucideDynamicIcon,
  LucideKeyRound,
  LucideSparkles,
  LucideWallet,
  type LucideIconInput,
} from '@lucide/angular';

interface AssistantStatusTile {
  id: string;
  label: string;
  icon: LucideIconInput;
  value: string;
  suffix: string;
  valueClass: string;
  barClass: string;
  barWidth: string;
  foot: string;
}

const resetFormat = dateTimeFormat({ day: 'numeric', month: 'long' });

@Component({
  selector: 'app-assistant-status-band',
  imports: [LucideDynamicIcon, PanelComponent],
  host: { class: 'block' },
  template: `
    <div class="grid grid-cols-1 gap-3 sm:grid-cols-3">
      @for (tile of tiles(); track tile.id) {
        <app-panel surface="card" class="flex flex-col gap-2.5 p-5">
          <div class="flex items-center justify-between gap-2">
            <span
              class="text-muted text-xs font-semibold tracking-wide uppercase">
              {{ tile.label }}
            </span>
            <svg
              [lucideIcon]="tile.icon"
              class="text-muted/60 h-3.5 w-3.5"
              aria-hidden="true"></svg>
          </div>

          <div class="flex items-baseline gap-2">
            <span
              class="font-overpass text-2xl font-semibold tabular-nums"
              [class]="tile.valueClass">
              {{ tile.value }}
            </span>
            <span class="text-muted text-sm">{{ tile.suffix }}</span>
          </div>

          <div class="bg-foreground/10 h-1 overflow-hidden rounded-full">
            <div
              class="h-full rounded-full"
              [class]="tile.barClass"
              [style.width]="tile.barWidth"></div>
          </div>

          <span class="text-muted text-xs">{{ tile.foot }}</span>
        </app-panel>
      }
    </div>
  `,
})
export class AssistantStatusBandComponent {
  readonly enabled = input.required<boolean>();
  readonly memberCount = input.required<number>();
  readonly spend = input.required<AiSpend | null>();
  readonly connected = input.required<number>();
  readonly totalConnections = input.required<number>();

  protected readonly tiles = computed<AssistantStatusTile[]>(() => {
    return [this.assistantTile(), this.spendTile(), this.connectionsTile()];
  });

  private assistantTile(): AssistantStatusTile {
    const enabled = this.enabled();
    const members = this.memberCount();
    const activeMembers = this.spend()?.members.length ?? 0;

    return {
      id: 'assistant',
      label: $localize`:Label of the assistant on-off status tile:Assistant`,
      icon: LucideSparkles,
      value: enabled
        ? $localize`:Shown when the assistant is on:On`
        : $localize`:Shown when the assistant is off:Off`,
      suffix: members
        ? $localize`:Says how many members the assistant serves:for ${members}:count: members`
        : '',
      valueClass: enabled ? '' : 'text-warn',
      barClass: enabled ? 'bg-primary' : 'bg-warn',
      barWidth: enabled ? '100%' : '0%',
      foot: enabled
        ? $localize`:Says how many members used the assistant this month:${activeMembers}:count: active this month`
        : $localize`:Explains what the assistant being off means:Members cannot send new messages`,
    };
  }

  private spendTile(): AssistantStatusTile {
    const spend = this.spend();
    const monthToDate = spend?.monthToDate ?? 0;
    const cap = spend?.cap ?? null;
    const percent = cap ? Math.min(100, (monthToDate / cap) * 100) : 0;
    const isNearCap = percent >= 90;

    return {
      id: 'spend',
      label: $localize`:Label of the assistant spend status tile:Spend`,
      icon: LucideWallet,
      value: formatCurrency(monthToDate),
      suffix: cap
        ? $localize`:Says what the spend is measured against:of ${formatCurrency(cap)}:cap:`
        : $localize`:Says the spend covers the current month:this month`,
      valueClass: isNearCap ? 'text-warn' : '',
      barClass: isNearCap ? 'bg-warn' : 'bg-primary',
      barWidth: `${percent}%`,
      foot: cap
        ? $localize`:Says when the spend period restarts:Resets ${this.resetLabel(spend)}:date:`
        : $localize`:Says the workspace has no assistant spend cap:No cap set`,
    };
  }

  private connectionsTile(): AssistantStatusTile {
    const connected = this.connected();
    const total = this.totalConnections();
    const percent = total ? (connected / total) * 100 : 0;
    const hasNone = connected === 0;

    return {
      id: 'connections',
      label: $localize`:Label of the assistant connections status tile:Connections`,
      icon: LucideKeyRound,
      value: `${connected}`,
      suffix: $localize`:Says how many connections are set up:of ${total}:total: connected`,
      valueClass: hasNone ? 'text-warn' : '',
      barClass: hasNone ? 'bg-warn' : 'bg-primary',
      barWidth: `${percent}%`,
      foot: hasNone
        ? $localize`:Explains that the assistant has no provider key:The assistant needs a provider key`
        : $localize`:Explains where personal keys fit in:Members can still bring their own key`,
    };
  }

  private resetLabel(spend: AiSpend | null): string {
    if (!spend) {
      return '';
    }

    return resetFormat.format(new Date(spend.periodEnd));
  }
}
