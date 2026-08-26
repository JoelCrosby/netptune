import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-keyboard-key',
  host: { class: 'contents' },
  template: `
    <kbd [class]="hostClass()">
      <ng-content />
    </kbd>
  `,
})
export class KeyboardKeyComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'border-border text-muted font-avatar inline-flex min-w-5 items-center justify-center rounded border px-1.5 py-0.5 text-[11px] leading-none font-medium',
      this.class()
    );
  });
}
