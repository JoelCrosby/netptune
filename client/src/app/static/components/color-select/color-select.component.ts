import { Component, input } from '@angular/core';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import {
  ColorDefinition,
  colorDictionary,
  resolveColorName,
} from '@core/util/colors/colors';
import { LucideCheck } from '@lucide/angular';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import {
  FormControlHintDirective,
  FormControlLabelDirective,
} from '../form-control/form-control.directives';
import { FormErrorComponent } from '../form-error/form-error.component';

@Component({
  selector: 'app-color-select',
  imports: [
    TooltipDirective,
    LucideCheck,
    FormControlLabelDirective,
    FormControlHintDirective,
    FormErrorComponent,
  ],
  template: `<div class="nept-form-control mb-[1.4rem] w-[inherit]">
    @if (label()) {
      <label [for]="name()" appFormLabel>
        {{ label() }}
      </label>
    }

    <div class="flex justify-stretch">
      @for (color of colorsRowTop; track color.name) {
        <div
          class="m-[0.2rem] flex h-9 min-h-9 min-w-9 flex-1 cursor-pointer items-center justify-center rounded-sm text-white"
          [appTooltip]="color.label"
          [class]="color.swatchClass"
          (click)="onOptionClicked(color)">
          @if (resolveColorName(value()) === color.name) {
            <svg lucideCheck class="h-6 w-6"></svg>
          }
        </div>
      }
    </div>
    <div class="flex justify-stretch">
      @for (color of colorsRowBottom; track color.name) {
        <div
          class="m-[0.2rem] flex h-9 min-h-9 min-w-9 flex-1 cursor-pointer items-center justify-center rounded-sm text-white"
          [appTooltip]="color.label"
          [class]="color.swatchClass"
          (click)="onOptionClicked(color)">
          @if (resolveColorName(value()) === color.name) {
            <svg lucideCheck class="h-6 w-6"></svg>
          }
        </div>
      }
    </div>

    @if (hint()) {
      <small appFormHint> {{ hint() }} </small>
    }

    @if (touched() && errors().length > 0) {
      @for (error of errors(); track error.kind) {
        <app-form-error>
          {{ error.message }}
        </app-form-error>
      }
    }
  </div> `,
})
export class ColorSelectComponent extends AbstractFormValueControl {
  readonly label = input.required<string>();
  readonly hint = input<string | null>(null);

  colors = colorDictionary();

  colorsRowTop = this.colors.slice(0, this.colors.length / 2);
  colorsRowBottom = this.colors.slice(this.colors.length / 2);

  readonly resolveColorName = resolveColorName;

  onOptionClicked(color: ColorDefinition) {
    this.value.set(color.name);
  }
}
