import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { avatarColors } from '@core/util/colors/colors';

import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { AvatarPipe } from '@static/pipes/avatar.pipe';

export type AvatarSize = 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TooltipDirective, AvatarPipe],
  template: `
    @if (pixelSize(); as pixelSize) {
      <div
        class="font-avatar inline-flex flex-col items-center justify-center overflow-hidden rounded-full text-xs font-medium tracking-[0.8px] whitespace-nowrap select-none"
        [style.height]="pixelSize"
        [style.max-height]="pixelSize"
        [style.width]="pixelSize"
        [style.max-width]="pixelSize"
        [style.background-color]="imageUrl() ? null : backgroundColor"
        [style.color]="color"
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
            class="h-full w-full object-cover"
            [src]="imageUrl()"
            [alt]="name()" />
        } @else {
          {{ name() | avatar }}
        }
      </div>
    }
  `,
})
export class AvatarComponent {
  readonly name = input<string | null>();
  readonly size = input<AvatarSize | undefined>('sm');
  readonly imageUrl = input<string | null>();
  readonly border = input(false);
  readonly tooltip = input(true);

  backgroundColor = avatarColors[0];
  color = '#fff';

  useLargeText = computed(() => Number(this.size()) >= 40);
  pixelSize = computed(() => {
    switch (this.size()) {
      case 'sm':
        return '24px';
      case 'md':
        return '32px';
      case 'lg':
        return '48px';
      case 'xl':
        return '96px';
      default:
        return '24px';
    }
  });
}
