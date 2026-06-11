import { Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-chip-listbox',
  template: '<ng-content />',
})
export class ChipListboxComponent {
  @HostBinding('class') className = 'flex flex-wrap gap-2';
}
