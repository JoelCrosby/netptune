import { Component, computed, input } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from './button/button.variants';

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
  readonly class = input('');

  protected readonly tileClass = computed(() =>
    cn(
      'bg-primary/10 text-primary flex shrink-0 items-center justify-center rounded-lg',
      sizeClasses[this.size()],
      this.class()
    )
  );

  protected readonly iconClass = computed(() => iconClasses[this.size()]);
}
