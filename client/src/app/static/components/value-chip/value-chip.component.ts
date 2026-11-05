import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { LucideX } from '@lucide/angular';
import { cva } from 'class-variance-authority';
import { cn } from '../button/button.variants';
import { IconButtonComponent } from '../button/icon-button.component';
import { ColorSwatchComponent } from '../color-swatch/color-swatch.component';

export type ValueChipTone = 'kept' | 'added' | 'removed' | 'empty';

export type ValueChipAccent =
  'none' | 'grey' | 'blue' | 'yellow' | 'orange' | 'red';

const valueChipVariants = cva(
  'inline-flex max-w-full min-w-0 items-center gap-1.5 rounded-full px-2.5 py-1 text-[13px] leading-tight',
  {
    variants: {
      tone: {
        kept: 'bg-foreground/6 text-foreground',
        added: 'bg-diff-add-word text-foreground',
        removed: 'bg-diff-del-word text-muted line-through',
        empty:
          'border-change-removed/50 text-muted border border-dashed bg-transparent italic',
      },
      accent: {
        none: '',
        grey: 'bg-zinc-400/15 font-medium text-zinc-500 dark:text-zinc-400',
        blue: 'bg-blue-400/15 font-medium text-blue-600 dark:text-blue-400',
        yellow:
          'bg-yellow-400/15 font-medium text-yellow-700 dark:text-yellow-400',
        orange:
          'bg-orange-400/15 font-medium text-orange-600 dark:text-orange-400',
        red: 'bg-red-500/15 font-medium text-red-600 dark:text-red-500',
      },
      mono: {
        true: 'font-mono',
        false: '',
      },
    },
    defaultVariants: {
      tone: 'kept',
      accent: 'none',
      mono: false,
    },
  }
);

@Component({
  selector: 'app-value-chip',
  imports: [ColorSwatchComponent, IconButtonComponent, LucideX],
  host: { '[class]': 'hostClass()' },
  template: `
    @if (color(); as color) {
      <app-color-swatch size="sm" [color]="color" />
    }
    <ng-content />
    <span class="min-w-0 truncate">{{ label() }}</span>
    @if (removeLabel(); as removeLabel) {
      <button
        app-icon-button
        class="-my-1 -mr-1.5 h-6 w-6"
        type="button"
        [ariaLabel]="removeLabel"
        (click)="removed.emit()">
        <svg lucideX class="h-3.5 w-3.5" aria-hidden="true"></svg>
      </button>
    }
  `,
})
export class ValueChipComponent {
  readonly label = input.required<string>();
  readonly color = input<string | null>(null);
  readonly tone = input<ValueChipTone>('kept');
  readonly accent = input<ValueChipAccent>('none');
  readonly mono = input(false, { transform: booleanAttribute });
  readonly removeLabel = input<string | null>(null);
  readonly class = input('');

  readonly removed = output();

  protected readonly hostClass = computed(() => {
    const variants = valueChipVariants({
      tone: this.tone(),
      accent: this.accent(),
      mono: this.mono(),
    });

    return cn(variants, this.class());
  });
}
