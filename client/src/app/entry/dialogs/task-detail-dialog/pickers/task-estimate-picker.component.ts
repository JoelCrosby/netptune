import { Component, input, output } from '@angular/core';
import {
  EstimateType,
  estimateTypeLabels,
  estimateTypeOptions,
  TaskEstimate,
  tShirtSizes,
} from '@core/enums/estimate-type';
import { LucideCheck } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { NumberInputComponent } from '@static/components/number-input/number-input.component';

@Component({
  selector: 'app-task-estimate-picker',
  imports: [
    DropdownMenuComponent,
    MenuItemComponent,
    NumberInputComponent,
    LucideCheck,
  ],
  template: `
    <button
      type="button"
      [class]="buttonClass()"
      [disabled]="disabled()"
      [attr.aria-label]="ariaLabel"
      aria-haspopup="menu"
      (click)="menu.toggle($any($event.currentTarget))">
      <ng-content />
    </button>

    <app-dropdown-menu #menu>
      <div class="w-56">
        <small class="text-muted block px-3 py-1 text-xs">
          <span i18n="Heading above the estimate-unit options">
            Estimate Type
          </span>
        </small>
        @for (option of estimateTypeOptions; track option.value) {
          <button app-menu-item (click)="selectEstimateType(option.value)">
            <span class="flex-1 text-left">{{ option.label }}</span>
            @if (option.value === currentType()) {
              <svg lucideCheck class="text-primary h-4 w-4"></svg>
            }
          </button>
        }

        <div class="border-foreground/8 my-1 border-t"></div>

        @if (currentType() === EstimateType.tShirt) {
          @for (size of tShirtSizes; track size.value) {
            <button
              app-menu-item
              (click)="selectEstimateValue(size.value); menu.close()">
              <span class="flex-1 text-left">{{ size.label }}</span>
              @if (size.value === estimateValue()) {
                <svg lucideCheck class="text-primary h-4 w-4"></svg>
              }
            </button>
          }
        } @else {
          <div class="px-3 py-2">
            <app-number-input
              [min]="0"
              [ariaLabel]="estimateTypeLabels[currentType()]"
              [value]="estimateValue()"
              (valueChange)="selectEstimateValue($event)" />
          </div>
        }
      </div>
    </app-dropdown-menu>
  `,
})
export class TaskEstimatePickerComponent {
  readonly estimateType = input<EstimateType | null>(null);
  readonly estimateValue = input<number | null>(null);
  readonly disabled = input(false);
  readonly buttonClass = input('');

  readonly estimateChange = output<TaskEstimate>();

  readonly EstimateType = EstimateType;
  readonly estimateTypeLabels = estimateTypeLabels;
  readonly estimateTypeOptions = estimateTypeOptions;
  readonly tShirtSizes = tShirtSizes;

  readonly ariaLabel = $localize`:Accessible label for the control that changes a task's estimate:Set estimate`;

  currentType() {
    return this.estimateType() ?? EstimateType.storyPoints;
  }

  selectEstimateType(estimateType: EstimateType) {
    if (estimateType === this.estimateType()) return;

    this.estimateChange.emit({ estimateType, estimateValue: null });
  }

  selectEstimateValue(estimateValue: number | null) {
    this.estimateChange.emit({
      estimateType: this.currentType(),
      estimateValue,
    });
  }
}
