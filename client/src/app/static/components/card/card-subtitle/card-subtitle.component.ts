import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-subtitle',
  templateUrl: './card-subtitle.component.html',
  styleUrls: ['./card-subtitle.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardSubtitleComponent {
  @HostBinding('class') className = 'netp-card-subtitle';
}
