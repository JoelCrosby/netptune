import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { avatarColors } from '@core/util/colors/colors';

import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { AvatarFontSizePipe } from '../../pipes/avatar-font-size.pipe';
import { AvatarPipe } from '../../pipes/avatar.pipe';
import { PxPipe } from '../../pipes/px.pipe';

@Component({
  selector: 'app-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TooltipDirective, AvatarPipe, AvatarFontSizePipe, PxPipe],
  template: `<div
    class="font-avatar flex flex-col items-center justify-center overflow-hidden border border-2 border-white/80 text-xs font-medium tracking-[0.8px] whitespace-nowrap select-none"
    [style.height]="size() | px"
    [style.width]="size() | px"
    [style.background-color]="imageUrl() ? null : backgroundColor"
    [style.color]="color"
    [style.border-radius]="borderRadius()"
    [class.avatar-border]="border()"
    [appTooltip]="tooltip() && name() ? name() : ''"
    [style.font-size]="size() | avatarFontSize">
    @if (imageUrl()) {
      <img
        crossorigin="anonymous"
        class="rounded-full object-cover"
        [width]="size()"
        [height]="size()"
        [src]="imageUrl()"
        [alt]="name()" />
    } @else {
      {{ name() | avatar }}
    }
  </div> `,
})
export class AvatarComponent {
  readonly name = input<string | null>();
  readonly size = input<(string | number | null) | undefined>('32');
  readonly imageUrl = input<string | null>();
  readonly border = input(false);
  readonly tooltip = input(true);
  readonly borderRadius = input<string | number>('50%');

  backgroundColor = avatarColors[0];
  color = '#fff';
}
