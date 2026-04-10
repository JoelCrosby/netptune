import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-chip-listbox',
  template: '<ng-content />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChipListboxComponent {
  @HostBinding('class') className = 'flex flex-wrap gap-2';
}
