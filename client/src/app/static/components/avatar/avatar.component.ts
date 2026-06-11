import { Component, computed, input } from '@angular/core';
import { avatarColors } from '@core/util/colors/colors';
import { cva, type VariantProps } from 'class-variance-authority';

import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { AvatarPipe } from '@static/pipes/avatar.pipe';

const avatarVariants = cva(
  'font-avatar flex flex-col items-center justify-center overflow-hidden rounded-full font-medium tracking-[0.8px] whitespace-nowrap select-none',
  {
    variants: {
      size: {
        sm: 'h-6 max-h-6 w-6 max-w-6 text-xs',
        md: 'h-8 max-h-8 w-8 max-w-8 text-xs',
        lg: 'h-12 max-h-12 w-12 max-w-12 text-xs',
        xl: 'h-24 max-h-24 w-24 max-w-24 text-xl font-bold',
      },
    },
    defaultVariants: {
      size: 'sm',
    },
  }
);

export type AvatarSize = NonNullable<
  VariantProps<typeof avatarVariants>['size']
>;

@Component({
  selector: 'app-avatar',
  imports: [TooltipDirective, AvatarPipe],
  template: `
    <div
      [class]="className()"
      [style.background-color]="imageUrl() ? null : backgroundColor"
      [style.color]="color"
      [class.avatar-border]="border()"
      [class.border]="border()"
      [class.border-2]="border()"
      [class.border-forground]="border()"
      [appTooltip]="tooltip() && name() ? name() : ''">
      @if (imageUrl()) {
        <img
          class="h-full w-full object-cover"
          [src]="imageUrl()"
          [alt]="name()" />
      } @else {
        {{ name() | avatar }}
      }
    </div>
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

  className = computed(() => avatarVariants({ size: this.size() }));
}
