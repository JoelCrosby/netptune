import { Component, computed, input, output } from '@angular/core';
import { AiModelOption } from '@core/models/ai-model';
import { LucideCheck } from '@lucide/angular';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-ai-assistant-model-menu',
  imports: [LucideCheck, DropdownButtonComponent, MenuItemComponent],
  template: `
    <app-dropdown-button
      #menu
      [label]="label()"
      i18n-ariaLabel="Accessible label for the assistant model selector"
      ariaLabel="Assistant model"
      buttonClass="h-8 max-w-52 rounded-full px-3 text-xs">
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
        <span i18n="Model option that lets the server choose">Automatic</span>
      </button>
      @for (model of models(); track model.id) {
        <button
          app-menu-item
          type="button"
          role="menuitemradio"
          [attr.aria-checked]="selectedModel() === model.id"
          (click)="selected.emit(model.id); menu.close()">
          <span class="flex h-4 w-4 items-center justify-center">
            @if (selectedModel() === model.id) {
              <svg lucideCheck class="h-4 w-4"></svg>
            }
          </span>
          <span>{{ model.label }}</span>
        </button>
      }
    </app-dropdown-button>
  `,
})
export class AiAssistantModelMenuComponent {
  readonly models = input.required<AiModelOption[]>();
  readonly selectedModel = input.required<string | null>();
  readonly label = input.required<string>();

  readonly selected = output<string | null>();

  protected readonly isAutomatic = computed(
    () => this.selectedModel() === null
  );
}
