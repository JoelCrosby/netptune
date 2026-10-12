import { Component, computed, input } from '@angular/core';
import { cn } from './button/button.variants';

export type PanelFooterTone = 'default' | 'muted';

const toneClasses: Record<PanelFooterTone, string> = {
  default: 'py-4',
  muted: 'bg-card-header text-foreground/50 py-3 text-xs',
};

@Component({
  selector: 'app-panel-footer',
  imports: [],
  host: {
    '[class]': 'hostClass()',
  },
  template: ` <ng-content /> `,
  styles: ``,
})
export class PanelFooterComponent {
  readonly tone = input<PanelFooterTone>('default');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'border-border block border-t px-6',
      toneClasses[this.tone()],
      this.class()
    );
  });
}
