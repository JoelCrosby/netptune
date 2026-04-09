import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

export type ButtonVariant = 'text' | 'filled' | 'outlined';
export type ButtonColor = 'primary' | 'warn' | '';

@Component({
  selector: 'app-button',
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || null"
      class="hover:bg-primary/60 inline-flex h-full w-full cursor-pointer items-center justify-center gap-2 border-0 bg-transparent font-[inherit] text-[inherit] outline-none disabled:cursor-not-allowed disabled:opacity-40">
      <ng-content />
    </button>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'hostClass()',
    '[attr.aria-disabled]': 'disabled() || null',
  },
  imports: [],
})
export class ButtonComponent {
  readonly iconOnly = input<boolean | string>(false);
  readonly variant = input<ButtonVariant>('text');
  readonly color = input<ButtonColor>('primary');
  readonly disabled = input(false);
  readonly type = input<'button' | 'submit' | 'reset'>('button');

  readonly hostClass = computed(() => {
    const base = 'inline-flex items-center justify-center transition-colors';

    if (this.iconOnly()) {
      const color = this.color();

      if (color === 'warn') {
        return `${base} h-10 w-10 rounded-full text-warn hover:bg-warn/8`;
      }

      if (color === 'primary') {
        return `${base} h-10 w-10 rounded-full text-primary hover:bg-primary/8`;
      }

      return `${base} h-10 w-10 rounded-full hover:bg-foreground/8`;
    }

    const sizing = 'h-9 px-4 text-sm font-medium rounded';
    const color = this.color();
    const variant = this.variant();

    if (variant === 'filled') {
      return color === 'warn'
        ? `${base} ${sizing} bg-warn text-white hover:bg-warn/90`
        : `${base} ${sizing} bg-primary text-white hover:bg-primary/90`;
    }

    if (variant === 'outlined') {
      return color === 'warn'
        ? `${base} ${sizing} border border-warn text-warn hover:bg-warn/8`
        : `${base} ${sizing} border border-primary text-primary hover:bg-primary/8`;
    }

    if (!color) {
      return `${base} ${sizing} text-foreground hover:bg-foreground/8`;
    }

    return color === 'warn'
      ? `${base} ${sizing} text-warn hover:bg-warn/8`
      : `${base} ${sizing} text-primary hover:bg-primary/8`;
  });
}
