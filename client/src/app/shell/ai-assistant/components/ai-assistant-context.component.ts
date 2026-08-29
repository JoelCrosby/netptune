import { Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiContextChip, contextChipKey } from '@core/models/ai-context';
import { LucideX } from '@lucide/angular';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-ai-assistant-context',
  host: { class: 'block' },
  imports: [LucideX, RouterLink, TooltipDirective],
  template: `
    @if (hasRow()) {
      <div
        class="mx-auto flex w-full flex-wrap items-center gap-1.5 px-5 pb-2"
        role="group"
        i18n-aria-label="
          Accessible name of the row showing what the assistant is told about
          the screen
        "
        aria-label="Sent with your message"
        [class]="contentWidth()">
        @for (chip of chips(); track key(chip)) {
          <span
            class="bg-hover text-muted flex items-center gap-1 rounded-full py-1 pr-1 pl-2.5 text-xs">
            <span>{{ chip.label }}</span>

            @if (chip.route; as route) {
              <a
                class="text-foreground max-w-40 truncate hover:underline"
                [routerLink]="route"
                [appTooltip]="chip.description">
                {{ chip.name }}
              </a>
            } @else {
              <span
                class="text-foreground max-w-40 truncate"
                [appTooltip]="chip.description">
                {{ chip.name }}
              </span>
            }

            <button
              type="button"
              class="hover:text-foreground flex h-4 w-4 items-center justify-center rounded-full transition-colors"
              [attr.aria-label]="removeLabel(chip)"
              (click)="removed.emit(chip)">
              <svg lucideX class="h-3 w-3"></svg>
            </button>
          </span>
        }

        @if (hasRemoved()) {
          @if (chips().length === 0) {
            <span
              class="text-muted text-xs"
              i18n="Shown when nothing about the screen is sent with a message">
              No context
            </span>
          }

          <button
            type="button"
            class="text-muted hover:text-foreground text-xs underline transition-colors"
            (click)="restored.emit()"
            i18n="Puts the removed context chips back">
            Restore
          </button>
        }
      </div>
    }
  `,
})
export class AiAssistantContextComponent {
  readonly chips = input.required<readonly AiContextChip[]>();
  readonly hasRemoved = input(false);
  readonly contentWidth = input('');

  readonly removed = output<AiContextChip>();
  readonly restored = output();

  protected readonly hasRow = computed(() => {
    return this.chips().length > 0 || this.hasRemoved();
  });

  protected key(chip: AiContextChip): string {
    return contextChipKey(chip);
  }

  protected removeLabel(chip: AiContextChip): string {
    return $localize`:Accessible label for the button that drops one thing from what is sent with a message:Remove ${chip.label}:KIND: ${chip.name}:NAME:`;
  }
}
