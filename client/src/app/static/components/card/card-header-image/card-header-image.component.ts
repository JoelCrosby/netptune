import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-header-image',
  templateUrl: './card-header-image.component.html',
  styleUrls: ['./card-header-image.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardHeaderImageComponent {
  @HostBinding('class') className = 'netp-card-header-image';
}
