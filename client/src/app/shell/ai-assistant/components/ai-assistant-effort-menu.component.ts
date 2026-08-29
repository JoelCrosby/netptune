import { Component, computed, input, output } from '@angular/core';
import { AiEffort, AiEffortOption } from '@core/models/ai-effort';
import { LucideCheck } from '@lucide/angular';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-ai-assistant-effort-menu',
  imports: [LucideCheck, DropdownButtonComponent, MenuItemComponent],
  template: `
    <app-dropdown-button
      #menu
      [label]="label()"
      i18n-ariaLabel="Accessible label for the assistant effort selector"
      ariaLabel="Assistant effort"
      buttonClass="h-8 max-w-40 rounded-full px-3 text-xs">
      <button
        app-menu-item
        type="button"
        role="menuitemradio"
        [attr.aria-checked]="isAutomatic()"
        (click)="selected.emit(null); menu.close()">
        <span class="flex h-4 w-4 items-center justify-center">
          @if (isAutomatic()) {
            <svg lucideCheck class="h-4 w-4"></svg>
          }
        </span>
        <span i18n="Effort option that lets the server choose">Automatic</span>
      </button>
      @for (option of efforts(); track option.effort) {
        <button
          app-menu-item
          type="button"
          role="menuitemradio"
          [attr.aria-checked]="selectedEffort() === option.effort"
          (click)="selected.emit(option.effort); menu.close()">
          <span class="flex h-4 w-4 items-center justify-center">
            @if (selectedEffort() === option.effort) {
              <svg lucideCheck class="h-4 w-4"></svg>
            }
          </span>
          <span>{{ option.label }}</span>
        </button>
      }
    </app-dropdown-button>
  `,
})
export class AiAssistantEffortMenuComponent {
  readonly efforts = input.required<AiEffortOption[]>();
  readonly selectedEffort = input.required<AiEffort | null>();
  readonly label = input.required<string>();

  readonly selected = output<AiEffort | null>();

  protected readonly isAutomatic = computed(
    () => this.selectedEffort() === null
  );
}
