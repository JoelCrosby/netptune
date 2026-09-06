import { Component, computed, input, output } from '@angular/core';
import { AiSpend } from '@core/models/ai-spend';
import { formatCurrency } from '@core/util/ai-usage';
import {
  LucideKeyRound,
  LucidePower,
  LucideTriangleAlert,
  type LucideIconInput,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';

export type AssistantBannerAction = 'add-key' | 'turn-on' | 'edit-cap';

interface AssistantBanner {
  icon: LucideIconInput;
  color: 'primary' | 'warn';
  title: string;
  body: string;
  action: string;
  kind: AssistantBannerAction;
}

@Component({
  selector: 'app-assistant-banner',
  imports: [CalloutComponent, FlatButtonComponent],
  host: { class: 'contents' },
  template: `
    @if (banner(); as message) {
      <app-callout
        [color]="message.color"
        [icon]="message.icon"
        class="[&>div]:items-center">
        <div
          class="flex flex-wrap items-center justify-between gap-x-4 gap-y-2">
          <div class="min-w-0">
            <p class="font-medium">{{ message.title }}</p>
            <p class="text-muted mt-0.5">{{ message.body }}</p>
          </div>
          <button
            app-flat-button
            type="button"
            class="h-8 shrink-0 px-3 text-xs"
            (click)="action.emit(message.kind)">
            {{ message.action }}
          </button>
        </div>
      </app-callout>
    }
  `,
})
export class AssistantBannerComponent {
  readonly canUpdateWorkspace = input.required<boolean>();
  readonly hasProviderKey = input.required<boolean>();
  readonly enabled = input.required<boolean>();
  readonly spend = input.required<AiSpend | null>();

  readonly action = output<AssistantBannerAction>();

  // Only one banner shows at a time, in the order an administrator would need
  // to act on them.
  protected readonly banner = computed<AssistantBanner | null>(() => {
    if (!this.canUpdateWorkspace()) {
      return null;
    }

    if (!this.hasProviderKey()) {
      return {
        icon: LucideKeyRound,
        color: 'primary',
        title: $localize`:Heading shown when no provider key is stored:No provider key yet`,
        body: $localize`:Explains why a workspace key helps:Add a workspace key so every member can use the assistant without bringing their own.`,
        action: $localize`:Button that adds a provider key:Add a key`,
        kind: 'add-key',
      };
    }

    if (!this.enabled()) {
      return {
        icon: LucidePower,
        color: 'warn',
        title: $localize`:Heading shown when the assistant is off:The assistant is turned off`,
        body: $localize`:Explains what the assistant being off means:Members cannot send new messages and pending changes will not apply. Keys and spend history are kept.`,
        action: $localize`:Button that turns the assistant back on:Turn back on`,
        kind: 'turn-on',
      };
    }

    return this.capBanner();
  });

  private capBanner(): AssistantBanner | null {
    const spend = this.spend();
    const cap = spend?.cap;

    if (!spend || !cap) {
      return null;
    }

    const percent = Math.round((spend.monthToDate / cap) * 100);

    if (percent < 90) {
      return null;
    }

    const remaining = formatCurrency(Math.max(0, cap - spend.monthToDate));

    return {
      icon: LucideTriangleAlert,
      color: 'warn',
      title: $localize`:Heading warning that the spend cap is nearly used up:${percent}:percent:% of the monthly cap used`,
      body: $localize`:Explains what happens when the assistant spend cap is reached:${remaining}:remaining: left of ${formatCurrency(cap)}:cap:. The assistant stops accepting new messages once the cap is reached.`,
      action: $localize`:Button that changes the assistant spend cap:Edit cap`,
      kind: 'edit-cap',
    };
  }
}
