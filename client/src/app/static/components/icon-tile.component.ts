import { booleanAttribute, Component, computed, input } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from './button/button.variants';
import { type Tone, toneClasses } from './tones';

export type IconTileSize = 'small' | 'medium' | 'large';

const sizeClasses: Record<IconTileSize, string> = {
  small: 'h-7 w-7',
  medium: 'h-9 w-9',
  large: 'h-10 w-10',
};

const iconClasses: Record<IconTileSize, string> = {
  small: 'h-3.5 w-3.5',
  medium: 'h-4 w-4',
  large: 'h-5 w-5',
};

const raisedClasses = 'rounded-xl bg-linear-to-br ring-1 ring-inset';

const raisedToneClasses: Record<Tone, string> = {
  neutral:
    'from-foreground/12 to-foreground/3 ring-foreground/8 text-foreground/70',
  blue: 'from-blue-500/20 to-blue-500/5 ring-blue-500/10 text-blue-700 dark:text-blue-300',
  purple: 'from-primary/20 to-primary/5 ring-primary/10 text-primary',
  amber:
    'from-amber-500/22 to-amber-500/6 ring-amber-500/12 text-amber-700 dark:text-amber-300',
  green:
    'from-emerald-500/22 to-emerald-500/6 ring-emerald-500/12 text-emerald-700 dark:text-emerald-300',
  teal: 'from-teal-500/22 to-teal-500/6 ring-teal-500/12 text-teal-700 dark:text-teal-300',
  pink: 'from-pink-500/22 to-pink-500/6 ring-pink-500/12 text-pink-700 dark:text-pink-300',
};

@Component({
  selector: 'app-icon-tile',
  imports: [LucideDynamicIcon],
  host: { class: 'contents' },
  template: `
    <span [class]="tileClass()" aria-hidden="true">
      <svg [lucideIcon]="icon()" [class]="iconClass()"></svg>
    </span>
  `,
})
export class IconTileComponent {
  readonly icon = input.required<LucideIconInput>();
  readonly size = input<IconTileSize>('medium');
  readonly tone = input<Tone>('purple');
  readonly raised = input(false, { transform: booleanAttribute });
  readonly class = input('');

  protected readonly tileClass = computed(() =>
    cn(
      'flex shrink-0 items-center justify-center rounded-lg',
      this.raised()
        ? [raisedClasses, raisedToneClasses[this.tone()]]
        : toneClasses[this.tone()],
      sizeClasses[this.size()],
      this.class()
    )
  );

  protected readonly iconClass = computed(() => iconClasses[this.size()]);
}
