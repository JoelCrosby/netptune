import { Component, input, output } from '@angular/core';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import { LucideCheck, LucideLayoutTemplate } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

interface DisplayModeOption {
  mode: AiDisplayMode;
  label: string;
}

const DISPLAY_MODE_OPTIONS: DisplayModeOption[] = [
  {
    mode: 'overlay',
    label: $localize`:Assistant display mode that floats over the page:Overlay`,
  },
  {
    mode: 'docked',
    label: $localize`:Assistant display mode that sits beside the page:Docked`,
  },
  {
    mode: 'dedicated',
    label: $localize`:Assistant display mode that fills its own page:Full page`,
  },
];

@Component({
  selector: 'app-ai-assistant-mode-menu',
  host: { class: 'inline-flex' },
  imports: [
    LucideCheck,
    LucideLayoutTemplate,
    DropdownMenuComponent,
    IconButtonComponent,
    MenuItemComponent,
    TooltipDirective,
  ],
  template: `
    <span #trigger class="inline-flex">
      <button
        app-icon-button
        type="button"
        class="rounded-full"
        aria-haspopup="menu"
        appTooltip
        i18n-appTooltip="Tooltip on the assistant display mode selector"
        appTooltip="Display mode"
        (click)="menu.toggle(trigger)">
        <svg lucideLayoutTemplate class="h-4 w-4"></svg>
      </button>
    </span>

    <app-dropdown-menu #menu xPosition="before">
      @for (option of options; track option.mode) {
        <button
          app-menu-item
          type="button"
          role="menuitemradio"
          [attr.aria-checked]="mode() === option.mode"
          (click)="modeChange.emit(option.mode); menu.close()">
          <span class="flex h-4 w-4 items-center justify-center">
            @if (mode() === option.mode) {
              <svg lucideCheck class="h-4 w-4"></svg>
            }
          </span>
          <span>{{ option.label }}</span>
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class AiAssistantModeMenuComponent {
  readonly mode = input.required<AiDisplayMode>();
  readonly modeChange = output<AiDisplayMode>();

  protected readonly options = DISPLAY_MODE_OPTIONS;
}
