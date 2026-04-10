import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'app-chip-option, button[app-chip-option]',
  template: '<ng-content />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChipOptionComponent {
  @HostBinding('class') className =
    'inline-flex items-center px-3 h-7 rounded-full text-xs font-medium border border-border bg-card-selected text-foreground select-none transition-colors hover:bg-card-selected/80 cursor-default';
}
