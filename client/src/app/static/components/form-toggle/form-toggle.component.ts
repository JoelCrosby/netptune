import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AbstractFormValueControl } from '../abstract-form-value-control';

@Component({
  selector: 'app-form-toggle',
  template: `
    <div class="nept-form-control">
      @if (label()) {
        <label [for]="name()" class="form-control-label">
          {{ label() }}
        </label>
      }

      <label class="form-toggle">
        <input type="checkbox" />
        <span class="form-toggle-slider round"></span>
      </label>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
})
export class FormToggleComponent extends AbstractFormValueControl {
  readonly label = input<string>();
}
