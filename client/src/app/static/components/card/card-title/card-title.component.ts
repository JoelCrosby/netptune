import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-title',
  templateUrl: './card-title.component.html',
  styleUrls: ['./card-title.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardTitleComponent {
  @HostBinding('class') className = 'netp-card-title';
}
