import { Component, computed, input } from '@angular/core';
import { cn } from './button/button.variants';

// `none` is for bodies that manage their own padding, such as a table or a
// list that needs to run to the panel edge.
export type PanelBodyPadding = 'comfortable' | 'compact' | 'snug' | 'none';

// Which edge separates this band from the one next to it.
export type PanelBodyDivider = 'none' | 'top' | 'bottom';

const paddingClasses: Record<PanelBodyPadding, string> = {
  comfortable: 'px-6 py-5',
  snug: 'px-6 py-4',
  compact: 'px-4 py-3',
  none: '',
};

const dividerClasses: Record<PanelBodyDivider, string> = {
  none: '',
  top: 'border-border border-t',
  bottom: 'border-border border-b',
};

@Component({
  // The attribute form keeps the semantic element for bands that are a
  // <header> or a <footer> in their own right.
  selector: 'app-panel-body, [app-panel-body]',
  imports: [],
  host: {
    '[class]': 'hostClass()',
  },
  template: ` <ng-content /> `,
  styles: ``,
})
export class PanelBodyComponent {
  readonly padding = input<PanelBodyPadding>('comfortable');
  readonly divider = input<PanelBodyDivider>('none');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'block',
      paddingClasses[this.padding()],
      dividerClasses[this.divider()],
      this.class()
    );
  });
}
