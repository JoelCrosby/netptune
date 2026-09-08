import { Component, computed, input } from '@angular/core';
import { cva } from 'class-variance-authority';
import { cn } from './button/button.variants';

export type ListRowTone = 'neutral' | 'primary';

const listRowVariants = cva(
  'flex items-center gap-3 rounded-lg border px-3 py-2',
  {
    variants: {
      tone: {
        neutral: 'border-border bg-foreground/2',
        primary: 'border-primary/25 bg-primary/8',
      },
    },
    defaultVariants: {
      tone: 'neutral',
    },
  }
);

// A single bordered row of related bits — a swatch, an id, a name, a trailing action.
// `app-selectable-row` is the pickable sibling; this one only displays.
@Component({
  selector: 'app-list-row, li[app-list-row]',
  template: '<ng-content />',
  host: { '[class]': 'hostClass()' },
})
export class ListRowComponent {
  readonly tone = input<ListRowTone>('neutral');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(listRowVariants({ tone: this.tone() }), this.class());
  });
}
