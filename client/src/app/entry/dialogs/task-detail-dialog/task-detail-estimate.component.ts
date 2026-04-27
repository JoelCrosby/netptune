import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EstimateType, estimateTypeLabels, estimateTypeOptions, tShirtSizes } from '@core/enums/estimate-type';
import { LucideChevronDown } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-detail-estimate',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DropdownMenuComponent, MenuItemComponent, FormsModule, LucideChevronDown],
  template: `
    <div class="flex items-center gap-2">
      <button
        class="flex cursor-pointer items-center gap-1 rounded px-2 py-1 text-sm transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800"
        (click)="typeMenu.toggle($any($event.currentTarget))">
        {{ estimateTypeLabels[estimateType() ?? EstimateType.storyPoints] }}
        <svg lucideChevronDown class="h-3 w-3 opacity-50"></svg>
      </button>
      <app-dropdown-menu #typeMenu>
        <small class="block px-3 py-1 text-xs text-neutral-500">Estimate Type</small>
        @for (opt of estimateTypeOptions; track opt.value) {
          <button
            app-menu-item
            (click)="onEstimateTypeChange(opt.value); typeMenu.close()">
            {{ opt.label }}
          </button>
        }
      </app-dropdown-menu>

      @if (estimateType() === EstimateType.tShirt) {
        <button
          class="flex cursor-pointer items-center gap-1 rounded px-2 py-1 text-sm transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800"
          (click)="sizeMenu.toggle($any($event.currentTarget))">
          {{ tShirtLabel() }}
          <svg lucideChevronDown class="h-3 w-3 opacity-50"></svg>
        </button>
        <app-dropdown-menu #sizeMenu>
          <small class="block px-3 py-1 text-xs text-neutral-500">Size</small>
          @for (size of tShirtSizes; track size.value) {
            <button
              app-menu-item
              (click)="estimateValueChange.emit(size.value); sizeMenu.close()">
              {{ size.label }}
            </button>
          }
        </app-dropdown-menu>
      } @else {
        <input
          type="number"
          min="0"
          class="w-20 rounded border border-neutral-200 bg-transparent px-2 py-1 text-sm dark:border-neutral-700"
          placeholder="—"
          [ngModel]="estimateValue()"
          (ngModelChange)="onEstimateValueChange($event)" />
      }
    </div>
  `,
})
export class TaskDetailEstimateComponent {
  readonly estimateType = input<EstimateType | null>(null);
  readonly estimateValue = input<number | null>(null);

  readonly estimateTypeChange = output<EstimateType>();
  readonly estimateValueChange = output<number | null>();

  readonly EstimateType = EstimateType;
  readonly estimateTypeLabels = estimateTypeLabels;
  readonly estimateTypeOptions = estimateTypeOptions;
  readonly tShirtSizes = tShirtSizes;

  tShirtLabel() {
    return tShirtSizes.find(s => s.value === this.estimateValue())?.label ?? '—';
  }

  onEstimateTypeChange(type: EstimateType) {
    this.estimateTypeChange.emit(type);
    this.estimateValueChange.emit(null);
  }

  onEstimateValueChange(value: number | null) {
    this.estimateValueChange.emit(value == null || isNaN(value) ? null : value);
  }
}
