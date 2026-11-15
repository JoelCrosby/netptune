import { Component, computed, input, output } from '@angular/core';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import { LucideSquarePen, LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { AiAssistantHistoryComponent } from './ai-assistant-history.component';
import { AiAssistantModeMenuComponent } from './ai-assistant-mode-menu.component';

@Component({
  selector: 'app-ai-assistant-header',
  host: {
    class: 'block shrink-0',
    '[class.h-15]': '!roomy()',
    '[class.border-b]': '!roomy()',
    '[class.border-border]': '!roomy()',
    '[class.h-16]': 'roomy()',
  },
  imports: [
    LucideSquarePen,
    LucideX,
    AiAssistantHistoryComponent,
    AiAssistantModeMenuComponent,
    IconButtonComponent,
    TooltipDirective,
  ],
  template: `
    <div
      class="mx-auto flex h-full w-full items-center justify-between gap-3"
      [class]="rowClass()">
      <div class="flex min-w-0 flex-col gap-px">
        <h2 class="truncate" [class]="titleClass()">
          {{ title() }}
        </h2>
        @if (subtitle(); as subtitle) {
          <span class="text-muted truncate text-[11.5px]">{{ subtitle }}</span>
        }
      </div>

      <div class="flex shrink-0 items-center gap-0.5">
        <app-ai-assistant-history />

        @if (hasModeMenu()) {
          <app-ai-assistant-mode-menu
            [mode]="mode()"
            (modeChange)="modeChange.emit($event)" />
        }

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
  readonly hasModeMenu = input(true);
  readonly roomy = input(false);

  protected readonly rowClass = computed(() => {
    const width = this.contentWidth();

    return this.roomy() ? `${width} pr-4 pl-6` : `${width} pr-2.5 pl-4.5`;
  });

  protected readonly titleClass = computed(() => {
    return this.roomy()
      ? 'text-[15px] font-medium'
      : 'text-[14.5px] font-semibold';
  });

  readonly modeChange = output<AiDisplayMode>();
  readonly newChat = output();
  readonly closed = output();
}
