import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-header-image',
  template: '<ng-content/>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardHeaderImageComponent {
  @HostBinding('class') className =
    'mr-[16px] flex items-center justify-center p-[.4rem] rounded-[var(--border-radius-small)] text-[#fff] opacity-[.6]';
}
