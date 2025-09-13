import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { avatarColors } from '@core/util/colors/colors';
import { MatTooltip } from '@angular/material/tooltip';
import { NgIf } from '@angular/common';
import { AvatarPipe } from '../../pipes/avatar.pipe';
import { AvatarFontSizePipe } from '../../pipes/avatar-font-size.pipe';
import { PxPipe } from '../../pipes/px.pipe';

@Component({
    selector: 'app-avatar',
    templateUrl: './avatar.component.html',
    styleUrls: ['./avatar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatTooltip, NgIf, AvatarPipe, AvatarFontSizePipe, PxPipe]
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
