import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { avatarColors } from '@core/util/colors/colors';

@Component({
    selector: 'app-avatar',
    templateUrl: './avatar.component.html',
    styleUrls: ['./avatar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class AvatarComponent {
  @Input() name?: string | null;
  @Input() size?: string | number | null = '32';
  @Input() imageUrl?: string | null;
  @Input() border = false;
  @Input() tooltip = true;
  @Input() borderRadius: string | number = '50%';

  backgroundColor = avatarColors[0];
  color = '#fff';
}
