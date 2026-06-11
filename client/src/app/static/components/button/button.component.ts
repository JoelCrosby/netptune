import { Component, computed, input } from '@angular/core';
import {
  buttonHostVariants,
  cn,
  coerceButtonColor,
  type ButtonColorInput,
  type ButtonVariant,
} from './button.variants';

export type {
  ButtonColor,
  ButtonColorInput,
  ButtonVariant,
} from './button.variants';

@Component({
  selector: 'app-button',
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || null"
      class="hover:bg-primary/60 inline-flex h-full w-full cursor-pointer items-center justify-center gap-2 border-0 bg-transparent font-[inherit] text-inherit outline-none disabled:cursor-not-allowed disabled:opacity-40">
      <ng-content />
    </button>
  `,
  host: {
    '[class]': 'hostClass()',
    '[attr.aria-disabled]': 'disabled() || null',
  },
  imports: [],
})
export class ButtonComponent {
  readonly iconOnly = input<boolean | string>(false);
  readonly variant = input<ButtonVariant>('text');
  readonly color = input<ButtonColorInput>('primary');
  readonly disabled = input(false);
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly class = input('');

  readonly hostClass = computed(() =>
    cn(
      buttonHostVariants({
        color: coerceButtonColor(this.color()),
        iconOnly: Boolean(this.iconOnly()),
        variant: this.variant(),
      }),
      this.class()
    )
  );
}
