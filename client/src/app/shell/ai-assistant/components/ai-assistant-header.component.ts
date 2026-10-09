import { Component, input, output } from '@angular/core';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import { LucideHistory, LucideSquarePen, LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { AiAssistantModeMenuComponent } from './ai-assistant-mode-menu.component';

@Component({
  selector: 'app-ai-assistant-header',
  host: { class: 'border-border block h-15 shrink-0 border-b' },
  imports: [
    LucideHistory,
    LucideSquarePen,
    LucideX,
    AiAssistantModeMenuComponent,
    IconButtonComponent,
    TooltipDirective,
  ],
  template: `
    <div
      class="mx-auto flex h-full w-full items-center justify-between gap-3 pr-2.5 pl-4.5"
      [class]="contentWidth()">
      <div class="flex min-w-0 flex-col gap-px">
        <h2 class="truncate text-[14.5px] font-semibold">
          {{ title() }}
        </h2>
        @if (subtitle(); as subtitle) {
          <span class="text-muted truncate text-[11.5px]">{{ subtitle }}</span>
        }
      </div>

      <div class="flex shrink-0 items-center gap-0.5">
        <button
          app-icon-button
          type="button"
          color="muted"
          size="small"
          i18n-appTooltip="Tooltip on the button that lists past conversations"
          appTooltip="Conversation history"
          appTooltipPosition="bottom"
          (click)="historyToggled.emit()">
          <svg lucideHistory class="h-4 w-4"></svg>
        </button>

        <app-ai-assistant-mode-menu
          [mode]="mode()"
          (modeChange)="modeChange.emit($event)" />

        <button
          app-icon-button
          type="button"
          color="muted"
          size="small"
          i18n-appTooltip="Tooltip on the button that starts a new chat"
          appTooltip="New chat"
          appTooltipPosition="bottom"
          (click)="newChat.emit()">
          <svg lucideSquarePen class="h-4 w-4"></svg>
        </button>

        @if (closable()) {
          <span
            class="bg-foreground/7 mx-1 h-4.5 w-px"
            aria-hidden="true"></span>
          <button
            app-icon-button
            type="button"
            color="muted"
            size="small"
            i18n-appTooltip="Tooltip on the button that closes the assistant"
            appTooltip="Close"
            appTooltipPosition="bottom"
            (click)="closed.emit()">
            <svg lucideX class="h-4 w-4"></svg>
          </button>
        }
      </div>
    </div>
  `,
})
export class AiAssistantHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
  readonly mode = input.required<AiDisplayMode>();
  readonly contentWidth = input('');
  readonly closable = input(false);

  readonly historyToggled = output();
  readonly modeChange = output<AiDisplayMode>();
  readonly newChat = output();
  readonly closed = output();
}
