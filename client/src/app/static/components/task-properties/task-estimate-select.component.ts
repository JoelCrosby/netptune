import { Component, input, output } from '@angular/core';
import {
  EstimateType,
  estimateTypeLabels,
  estimateTypeOptions,
  tShirtSizes,
} from '@core/enums/estimate-type';
import { LucideChevronDown } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { NumberInputComponent } from '@static/components/number-input/number-input.component';

export interface TaskEstimate {
  estimateType: EstimateType | null;
  estimateValue: number | null;
}

@Component({
  selector: 'app-task-estimate-select',
  imports: [
    DropdownMenuComponent,
    MenuItemComponent,
    NumberInputComponent,
    LucideChevronDown,
  ],
  template: `
    <div class="flex items-center gap-2">
      <button
        type="button"
        class="flex cursor-pointer items-center gap-1 rounded-sm px-4 py-2 text-sm transition-colors hover:bg-neutral-100 disabled:cursor-default disabled:hover:bg-transparent dark:hover:bg-neutral-800 dark:disabled:hover:bg-transparent"
        [disabled]="disabled()"
        (click)="typeMenu.toggle($any($event.currentTarget))">
        {{ estimateTypeLabels[estimateType() ?? EstimateType.storyPoints] }}
        @if (!disabled()) {
          <svg lucideChevronDown class="h-3 w-3 opacity-50"></svg>
        }
      </button>
      <app-dropdown-menu #typeMenu>
        <small class="block px-3 py-1 text-xs text-neutral-500">
          <span i18n="Heading above the estimate-unit options">
            Estimate Type
          </span>
        </small>
        @for (opt of estimateTypeOptions; track opt.value) {
          <button
            app-menu-item
            (click)="selectEstimateType(opt.value); typeMenu.close()">
            {{ opt.label }}
          </button>
        }
      </app-dropdown-menu>

      @if (estimateType() === EstimateType.tShirt) {
        <button
          type="button"
          class="flex cursor-pointer items-center gap-1 rounded-sm px-4 py-2 text-sm transition-colors hover:bg-neutral-100 disabled:cursor-default disabled:hover:bg-transparent dark:hover:bg-neutral-800 dark:disabled:hover:bg-transparent"
          [disabled]="disabled()"
          (click)="sizeMenu.toggle($any($event.currentTarget))">
          {{ tShirtLabel() }}
          @if (!disabled()) {
            <svg lucideChevronDown class="h-3 w-3 opacity-50"></svg>
          }
        </button>
        <app-dropdown-menu #sizeMenu>
          <small class="block px-3 py-1 text-xs text-neutral-500">
            <span i18n="Heading above the t-shirt-size estimate options">
              Size
            </span>
          </small>
          @for (size of tShirtSizes; track size.value) {
            <button
              app-menu-item
              (click)="selectEstimateValue(size.value); sizeMenu.close()">
              {{ size.label }}
            </button>
          }
        </app-dropdown-menu>
      } @else {
        <app-number-input
          class="w-32"
          [min]="0"
          [ariaLabel]="
            estimateTypeLabels[estimateType() ?? EstimateType.storyPoints]
          "
          [disabled]="disabled()"
          [value]="estimateValue()"
          (valueChange)="selectEstimateValue($event)" />
      }
    </div>
  `,
})
export class TaskEstimateSelectComponent {
  readonly estimateType = input<EstimateType | null>(null);
  readonly estimateValue = input<number | null>(null);
  readonly disabled = input(false);

  readonly estimateChange = output<TaskEstimate>();

  readonly EstimateType = EstimateType;
  readonly estimateTypeLabels = estimateTypeLabels;
  readonly estimateTypeOptions = estimateTypeOptions;
  readonly tShirtSizes = tShirtSizes;

  tShirtLabel() {
    return (
      tShirtSizes.find((s) => s.value === this.estimateValue())?.label ?? '—'
    );
  }

  selectEstimateType(value: EstimateType) {
    const estimateType = value == null || isNaN(value) ? null : value;

    this.estimateChange.emit({ estimateType, estimateValue: null });
  }

  selectEstimateValue(estimateValue: number | null) {
    this.estimateChange.emit({
      estimateType: this.estimateType(),
      estimateValue,
    });
  }
}
