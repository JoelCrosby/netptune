import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { colorDictionary, NamedColor } from '@core/util/colors/colors';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import { AbstractFormValueControl } from '../abstract-form-value-control';

@Component({
  selector: 'app-color-select',
  templateUrl: './color-select.component.html',
  styleUrls: ['./color-select.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTooltip, MatIcon],
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
