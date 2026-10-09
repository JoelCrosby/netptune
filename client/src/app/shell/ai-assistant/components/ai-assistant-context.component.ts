import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiContextChip, contextChipKey } from '@core/models/ai-context';
import { LucidePlus, LucideX } from '@lucide/angular';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-ai-assistant-context',
  host: { class: 'block' },
  imports: [LucidePlus, LucideX, RouterLink, TooltipDirective],
  template: `
    <div
      class="flex flex-wrap items-center gap-1.5"
      role="group"
      i18n-aria-label="
        Accessible name of the row showing what the assistant is told about the
        screen
      "
      aria-label="Sent with your message">
      @for (chip of chips(); track key(chip)) {
        <span
          class="bg-foreground/5 text-foreground/70 flex h-6 items-center gap-1.5 rounded-md pr-1 pl-2 text-[11.5px]">
          <span class="text-muted">{{ chip.label }}</span>

          @if (chip.route; as route) {
            <a
              class="max-w-40 truncate font-semibold hover:underline"
              [routerLink]="route"
              [appTooltip]="chip.description">
              {{ chip.name }}
            </a>
          } @else {
            <span
              class="max-w-40 truncate font-semibold"
              [appTooltip]="chip.description">
              {{ chip.name }}
            </span>
          }

          <button
            type="button"
            class="text-muted hover:bg-foreground/5 hover:text-foreground flex h-4 w-4 items-center justify-center rounded transition-colors"
            [attr.aria-label]="removeLabel(chip)"
            (click)="removed.emit(chip)">
            <svg lucideX class="h-2.5 w-2.5" strokeWidth="2.6"></svg>
          </button>
        </span>
      }

      @if (hasRemoved()) {
        <button
          type="button"
          class="border-foreground/15 text-muted hover:text-foreground flex h-6 items-center gap-1 rounded-md border border-dashed px-1.5 text-[11.5px] transition-colors"
          i18n-appTooltip="Tooltip on the button that puts removed context back"
          appTooltip="Restore removed context"
          (click)="restored.emit()">
          <svg lucidePlus class="h-2.75 w-2.75" strokeWidth="2.4"></svg>
          <span i18n="Puts the removed context chips back">Context</span>
        </button>
      }
    </div>
  `,
})
export class AiAssistantContextComponent {
  readonly chips = input.required<readonly AiContextChip[]>();
  readonly hasRemoved = input(false);

  readonly removed = output<AiContextChip>();
  readonly restored = output();

  protected key(chip: AiContextChip): string {
    return contextChipKey(chip);
  }

  protected removeLabel(chip: AiContextChip): string {
    return $localize`:Accessible label for the button that drops one thing from what is sent with a message:Remove ${chip.label}:KIND: ${chip.name}:NAME:`;
  }
}
