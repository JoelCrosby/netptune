import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-subtitle',
  template: '<ng-content/>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardSubtitleComponent {
  @HostBinding('class') className =
    'block rounded-[4px] px-[.4rem] py-[.2rem] h-4 text-[14px] mr-[.6rem] mt-auto text-neutral-900 dark:text-white';
}
