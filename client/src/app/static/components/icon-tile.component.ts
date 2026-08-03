import { Component, computed, input } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from './button/button.variants';

export type IconTileSize = 'medium' | 'large';

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
      this.size() === 'large' ? 'h-10 w-10' : 'h-9 w-9',
      this.class()
    )
  );

  protected readonly iconClass = computed(() =>
    this.size() === 'large' ? 'h-5 w-5' : 'h-4 w-4'
  );
}
