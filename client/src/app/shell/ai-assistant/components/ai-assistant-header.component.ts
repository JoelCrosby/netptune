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
      class="mx-auto flex h-full w-full items-center justify-between gap-3 px-4"
      [class]="contentWidth()">
      <div class="min-w-0">
        <h2 class="font-overpass truncate text-[1.05rem] font-medium">
          {{ title() }}
        </h2>
        <p class="text-muted truncate text-xs">{{ subtitle() }}</p>
      </div>

      <div class="flex shrink-0 items-center gap-1">
        <button
          app-icon-button
          type="button"
          class="rounded-full"
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
          class="rounded-full"
          i18n-appTooltip="Tooltip on the button that starts a new chat"
          appTooltip="New chat"
          appTooltipPosition="bottom"
          (click)="newChat.emit()">
          <svg lucideSquarePen class="h-4 w-4"></svg>
        </button>

        @if (closable()) {
          <button
            app-icon-button
            type="button"
            class="rounded-full"
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
  readonly subtitle = input.required<string>();
  readonly mode = input.required<AiDisplayMode>();
  readonly contentWidth = input('');
  readonly closable = input(false);

  readonly historyToggled = output();
  readonly modeChange = output<AiDisplayMode>();
  readonly newChat = output();
  readonly closed = output();
}
