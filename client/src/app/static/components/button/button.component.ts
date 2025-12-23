import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-button',
  template: `<button class="form-button" [class.icon-only]="iconOnly()">
    <ng-content />
  </button>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
})
export class ButtonComponent {
  iconOnly = input<boolean | string>(false);
}
