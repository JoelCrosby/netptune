import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { avatarColors } from '@core/util/colors/colors';

import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { AvatarPipe } from '@static/pipes/avatar.pipe';
import { PxPipe } from '@static/pipes/px.pipe';

@Component({
  selector: 'app-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TooltipDirective, AvatarPipe, PxPipe],
  template: `<div
    class="font-avatar flex flex-col items-center justify-center overflow-hidden text-xs font-medium tracking-[0.8px] whitespace-nowrap select-none"
    [style.height]="size() | px"
    [style.width]="size() | px"
    [style.background-color]="imageUrl() ? null : backgroundColor"
    [style.color]="color"
    [style.border-radius]="borderRadius()"
    [class.avatar-border]="border()"
    [class.border]="border()"
    [class.border-2]="border()"
    [class.border-forground]="border()"
    [appTooltip]="tooltip() && name() ? name() : ''"
    [class.text-xl!]="useLargeText()"
    [class.font-bold!]="useLargeText()">
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

  useLargeText = computed(() => Number(this.size()) >= 40);
}
