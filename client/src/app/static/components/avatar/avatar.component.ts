import { ChangeDetectionStrategy, Component, Input, input } from '@angular/core';
import { avatarColors } from '@core/util/colors/colors';
import { MatTooltip } from '@angular/material/tooltip';

import { AvatarPipe } from '../../pipes/avatar.pipe';
import { AvatarFontSizePipe } from '../../pipes/avatar-font-size.pipe';
import { PxPipe } from '../../pipes/px.pipe';

@Component({
    selector: 'app-avatar',
    templateUrl: './avatar.component.html',
    styleUrls: ['./avatar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatTooltip, AvatarPipe, AvatarFontSizePipe, PxPipe]
})
export class AvatarComponent {
  readonly name = input<string | null>();
  readonly size = input<(string | number | null) | undefined>('32');
  @Input() imageUrl?: string | null;
  readonly border = input(false);
  readonly tooltip = input(true);
  readonly borderRadius = input<string | number>('50%');

  backgroundColor = avatarColors[0];
  color = '#fff';
}
