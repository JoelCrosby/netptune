import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-menu-item]',
  template: '<ng-content />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MenuItemComponent {
  @HostBinding('class') readonly className =
    'flex w-full items-center gap-3 px-3 py-2 text-sm text-left cursor-pointer select-none rounded-sm transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800 focus-visible:outline-none focus-visible:bg-neutral-100 dark:focus-visible:bg-neutral-800 disabled:pointer-events-none disabled:opacity-50';
}
