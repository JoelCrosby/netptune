import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-group',
  templateUrl: './card-group.component.html',
  styleUrls: ['./card-group.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardGroupComponent {
  @HostBinding('class') className = 'netp-card-group';
}
