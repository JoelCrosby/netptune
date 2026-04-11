import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { colorDictionary, NamedColor } from '@core/util/colors/colors';
import { LucideCheck } from '@lucide/angular';
import { AbstractFormValueControl } from '../abstract-form-value-control';

@Component({
  selector: 'app-color-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TooltipDirective, LucideCheck],
  template: `<div class="nept-form-control">
    @if (label()) {
      <label [for]="name()" class="form-control-label">
        {{ label() }}
      </label>
    }

    <div class="flex justify-stretch">
      @for (color of colorsRowTop; track color) {
        <div
          class="m-[0.2rem] flex h-9 min-h-9 min-w-9 flex-1 cursor-pointer items-center justify-center rounded-[--radius-sm] text-white"
          [appTooltip]="color.name"
          [style.background-color]="color.color"
          (click)="onOptionClicked(color)">
          @if (value() === color.color) {
            <svg lucideCheck class="h-4 w-4"></svg>
          }
        </div>
      }
    </div>
    <div class="flex justify-stretch">
      @for (color of colorsRowBottom; track color.color) {
        <div
          class="m-[0.2rem] flex h-9 min-h-9 min-w-9 flex-1 cursor-pointer items-center justify-center rounded-[--radius-sm] text-white"
          [appTooltip]="color.name"
          [style.background-color]="color.color"
          (click)="onOptionClicked(color)">
          @if (value() === color.color) {
            <svg lucideCheck class="h-4 w-4"></svg>
          }
        </div>
      }
    </div>

    @if (hint()) {
      <small class="form-control-hint"> {{ hint() }} </small>
    }
  </div> `,
})
export class ColorSelectComponent extends AbstractFormValueControl {
  readonly label = input.required<string>();
  readonly hint = input<string | null>(null);

  colors = colorDictionary();

  colorsRowTop = this.colors.slice(0, this.colors.length / 2);
  colorsRowBottom = this.colors.slice(
    this.colors.length / 2,
    this.colors.length - 1
  );

  onOptionClicked(color: NamedColor) {
    this.value.set(color.color);
  }
}
