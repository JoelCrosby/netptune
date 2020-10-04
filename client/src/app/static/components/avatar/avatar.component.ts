import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { avatarColors } from '@core/util/colors/colors';

@Component({
  selector: 'app-avatar',
  templateUrl: './avatar.component.html',
  styleUrls: ['./avatar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvatarComponent {
  @Input() name: string;
  @Input() size: string | number = '32';
  @Input() imageUrl: string;
  @Input() border = false;
  @Input() tooltip = true;
  @Input() borderRadius: string | number = '50%';

  backgroundColor = avatarColors[0];
  color = '#fff';
}
