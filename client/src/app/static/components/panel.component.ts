import { Component, computed, input } from '@angular/core';
import { cn } from './button/button.variants';

// `background` sits flush with the page; `card` reads as a raised surface.
export type PanelSurface = 'background' | 'card';

// For components that are themselves a panel and carry the surface on their
// host rather than wrapping their template in one.
export const panelSurfaceClass =
  'border-border block rounded-lg border shadow-sm';

@Component({
  // The attribute form keeps the semantic element when a panel is also a
  // <form> or a landmark.
  selector: 'app-panel, [app-panel]',
  imports: [],
  host: {
    '[class]': 'hostClass()',
  },
  template: ` <ng-content /> `,
  styles: ``,
})
export class PanelComponent {
  readonly surface = input<PanelSurface>('background');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      panelSurfaceClass,
      'overflow-hidden',
      this.surface() === 'card' ? 'bg-card' : 'bg-background',
      this.class()
    );
  });
}
